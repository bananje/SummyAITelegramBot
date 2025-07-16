using Cronos;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using SummyAITelegramBot.Core.Abstractions;
using SummyAITelegramBot.Core.AI.Abstractions;
using SummyAITelegramBot.Core.Bot.Features.Channel.Abstractions;
using SummyAITelegramBot.Core.Domain.Enums;
using SummyAITelegramBot.Core.Domain.Models;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using UserEn = SummyAITelegramBot.Core.Domain.Models.User;

namespace SummyAITelegramBot.Core.Bot.Features.Channel.Services;

public class TelegramSenderService(
    ITelegramBotClient telegramBotClient,
    ISummarizationStrategyFactory aiFactory,
    IStaticImageService imageService,
    IUnitOfWork unitOfWork) : ITelegramSenderService
{
    public async Task ResolveNotifyUsersAsync(ChannelPost post)
    {
        var userRepository = unitOfWork.Repository<long, UserEn>();
        var users = await userRepository.GetIQueryable()
            .Where(u => u.Channels.Any(c => c.Id == post.ChannelId))
            .Include(u => u.ChannelUserSettings)
            .ToListAsync();

        var delayedRepo = unitOfWork.Repository<long, DelayedUserPost>();
        var sentPostsRepo = unitOfWork.Repository<int, SentUserPost>();

        foreach (var user in users)
        {
            var settings = user.ChannelUserSettings ?? throw new Exception($"Отсутсвуют настройки у пользователя ID:{user.Id}");

            if (settings.InstantlyTimeNotification.GetValueOrDefault())
            {
                // Получим посты за последние 10 минут
                var recentPosts = await sentPostsRepo.GetIQueryable()
                    .Where(p => p.UserId == user.Id &&
                                p.SentAt >= DateTime.UtcNow.AddMinutes(-10))
                    .Include(p => p.ChannelPost)
                    .ToListAsync();

                if (recentPosts.Count == 0)
                {
                    var messageText = FormatInstantSummary(post);

                    await using var stream = imageService.GetImageStream(post.MediaPath, "media_cache");

                    await telegramBotClient.SendPhoto(
                        chatId: user.Id,
                        photo: new InputFileStream(stream),
                        caption: messageText,
                        parseMode: ParseMode.Html);

                    await sentPostsRepo.AddAsync(new SentUserPost
                    {
                        UserId = user.Id,
                        ChannelId = post.ChannelId,
                        ChannelPostId = post.Id,
                        SentAt = DateTime.UtcNow
                    });
                    await unitOfWork.CommitAsync();
                    return;
                }

                // Объединяем все тексты
                var allTexts = new List<string>(recentPosts.Select(p => p.ChannelPost.Text));
                var currentPostText = post.Text;
                var message = string.Join("\n\n", allTexts);

                var aiHandler = aiFactory.Create(AiModel.DeepSeek);
                var isUniquePost = await aiHandler.ValidateOfUniqueTextAsync(message, currentPostText);

                if (isUniquePost)
                {
                    var messageText = FormatInstantSummary(post);

                    await using var stream = imageService.GetImageStream(post.MediaPath, "media_cache");

                    await telegramBotClient.SendPhoto(
                        chatId: user.Id, 
                        caption: messageText,
                        parseMode: ParseMode.Html,
                        photo: new InputFileStream(stream));

                    await sentPostsRepo.AddAsync(new SentUserPost
                    {
                        UserId = user.Id,
                        ChannelId = post.ChannelId,
                        ChannelPostId = post.Id,
                        SentAt = DateTime.UtcNow
                    });
                }
            }
            else
            {
                await delayedRepo.AddAsync(new DelayedUserPost
                {
                    UserId = user.Id,
                    ChannelId = post.ChannelId,
                    CreatedDate = DateTime.UtcNow,
                    ChannelPostId = post.Id
                });
            }
        }

        await unitOfWork.CommitAsync();

        // Поставить задачу в Hangfire для пользователей с отложенными сообщениями
        foreach (var user in users.Where(u => !u.ChannelUserSettings.InstantlyTimeNotification.GetValueOrDefault()))
        {
            ScheduleOneTimeJob(user);
        }
    }

    public async Task SendGroupedPostsAsync(long userId, int page)
    {
        var delayedRepo = unitOfWork.Repository<long, DelayedUserPost>();
        var posts = await delayedRepo.GetIQueryable()
            .Where(p => p.UserId == userId)
            .Include(p => p.ChannelPost)
            .ThenInclude(cp => cp.Channel)
            .ToListAsync();

        var grouped = posts
            .GroupBy(p => p.ChannelPost.ChannelId)
            .OrderBy(g => g.Key)
            .ToList();

        if (!grouped.Any())
            return;

        var current = grouped.ElementAtOrDefault(page);
        if (current == null)
            return;

        var channel = current.First().ChannelPost.Channel;
        var channelUrl = channel.Link?.Trim();
        var channelTitle = channel.Title?.Trim() ?? "Канал";

        // Извлекаем username из URL: https://t.me/username -> username
        var channelUsername = channelUrl?
            .Replace("https://t.me/", "", StringComparison.OrdinalIgnoreCase)
            .TrimEnd('/');

        var sb = new StringBuilder();

        sb.AppendLine("🗂 <b>Групповая сводка по каналу</b>\n");

        if (!string.IsNullOrEmpty(channelUrl) && !string.IsNullOrEmpty(channelTitle))
            sb.AppendLine($"<b>📢 <a href=\"{channelUrl}\">{channelTitle}</a></b>\n");
        else
            sb.AppendLine($"<b>📢 {channelTitle}</b>\n");

        foreach (var postEntry in current)
        {
            var post = postEntry.ChannelPost;
            var postText = post.Text?.Trim() ?? "(без текста)";

            // Ссылка на конкретный пост
            string? postLink = !string.IsNullOrEmpty(channelUsername)
                ? $"https://t.me/{channelUsername}/{post.Id}"
                : null;

            sb.AppendLine($"• {postText}");
            if (postLink != null)
                sb.AppendLine($"<a href=\"{postLink}\">🔍 Подробнее</a>");
            sb.AppendLine();
        }

        var navButtons = new List<InlineKeyboardButton>();
        if (page > 0)
            navButtons.Add(InlineKeyboardButton.WithCallbackData("◀ Назад", $"/groupedposts:{page - 1}"));
        if (page < grouped.Count - 1)
            navButtons.Add(InlineKeyboardButton.WithCallbackData("▶ Далее", $"/groupedposts:{page + 1}"));

        var markup = navButtons.Any() ? new InlineKeyboardMarkup(navButtons) : null;

        await telegramBotClient.SendMessage(
            chatId: userId,
            text: sb.ToString(),
            parseMode: ParseMode.Html,
            replyMarkup: markup,
            linkPreviewOptions: true
        );
    }

    private void ScheduleOneTimeJob(UserEn user)
    {
        if (user.ChannelUserSettings?.NotificationTime == null)
            return;

        var timeZoneId = user.ChannelUserSettings.TimeZoneId ?? "UTC";
        TimeZoneInfo timeZone;

        try
        {
            timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch
        {
            timeZone = TimeZoneInfo.Utc;
        }

        var notificationTime = user.ChannelUserSettings.NotificationTime.Value;

        // Создаем cron-выражение для времени из настроек пользователя (ежедневно)
        var cronExpression = Cron.Daily(notificationTime.Hour, notificationTime.Minute);

        var cronSchedule = CronExpression.Parse(cronExpression);

        // Текущее время в UTC
        var utcNow = DateTimeOffset.UtcNow;

        // Получаем следующее время запуска в UTC с учётом cron и часового пояса
        var nextUtc = cronSchedule.GetNextOccurrence(utcNow, TimeZoneInfo.Utc);

        if (!nextUtc.HasValue)
            return; // Нет следующего запуска — не ставим задачу

        // Конвертируем следующий запуск в локальное время пользователя
        var nextRunLocal = TimeZoneInfo.ConvertTime(nextUtc.Value, timeZone);

        // Вычисляем задержку от текущего локального времени
        var delay = nextRunLocal - DateTimeOffset.Now;

        if (delay < TimeSpan.Zero)
            delay = TimeSpan.Zero;

        BackgroundJob.Schedule<TelegramSenderService>(
            service => service.SendGroupedPostsAsync(user.Id, 0),
            delay
        );
    }

    private string FormatInstantSummary(ChannelPost post)
    {
        var channelTitle = post.Channel?.Title?.Trim() ?? "Канал";
        var postText = string.IsNullOrWhiteSpace(post.Text) ? "Нет текста" : post.Text.Trim();
        var channelUrlRaw = post.Channel?.Link?.Trim();

        // Извлекаем username из ссылки канала (https://t.me/username)
        var channelUsername = !string.IsNullOrEmpty(channelUrlRaw)
            ? channelUrlRaw.Replace("https://t.me/", "", StringComparison.OrdinalIgnoreCase).TrimEnd('/')
            : null;

        if (string.IsNullOrEmpty(channelUsername) || post.Id == 0)
        {
            return $"""
        📬 <b>Новая сводка!</b>

        <b>📢 {channelTitle}</b>

        {postText}
        """;
        }

        var channelUrl = $"https://t.me/{channelUsername}";
        var postUrl = $"https://t.me/{channelUsername}/{post.Id}";

        return $"""
    📬 <b>Новая сводка!</b>

    <b>📢 <a href="{channelUrl}">{channelTitle}</a></b>

    {postText}

    <a href="{postUrl}">👁‍🗨 Посмотреть полностью</a>
    """;
    }
}