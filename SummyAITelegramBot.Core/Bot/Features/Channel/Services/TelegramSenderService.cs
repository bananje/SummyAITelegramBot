using Hangfire;
using Microsoft.EntityFrameworkCore;
using SummyAITelegramBot.Core.Abstractions;
using SummyAITelegramBot.Core.AI.Abstractions;
using SummyAITelegramBot.Core.Bot.Extensions;
using SummyAITelegramBot.Core.Bot.Features.Channel.Abstractions;
using SummyAITelegramBot.Core.Domain.Enums;
using SummyAITelegramBot.Core.Domain.Models;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using UserEn = SummyAITelegramBot.Core.Domain.Models.User;

namespace SummyAITelegramBot.Core.Bot.Features.Channel.Services;

public class TelegramSenderService(
    ITelegramBotClient telegramBotClient,
    ISummarizationStrategyFactory aiFactory,
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
                    await telegramBotClient.SendMessage(chatId: user.Id, text: "push " + post.Text);
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
                var allTexts = new List<string>();
                allTexts.AddRange(recentPosts.Select(p => p.ChannelPost.Text));

                var currentPostText = post.Text;
                var message = string.Join("\n\n", allTexts);

                var aiHandler = aiFactory.Create(AiModel.DeepSeek);
                var isUniquePost = await aiHandler.ValidateOfUniqueTextAsync(message, currentPostText);

                if (isUniquePost)
                {
                    await telegramBotClient.SendMessage(chatId: user.Id, text: "push " + post.Text);

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
                    ChannelPostId = post.Id
                });
            }
        }

        await unitOfWork.CommitAsync();

        // Поставить задачу в Hangfire для пользователей с отложенными сообщениями (см. ниже)
        foreach (var user in users.Where(u => !u.ChannelUserSettings.InstantlyTimeNotification.GetValueOrDefault()))
        {
            await ScheduleNotificationJobIfNeeded(user);
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
        {
            await telegramBotClient.SendMessage(userId, "Нет новых постов");
            return;
        }

        var current = grouped.ElementAtOrDefault(page);
        if (current == null)
        {
            await telegramBotClient.SendMessage(userId, "Страница не найдена");
            return;
        }

        var text = $"""
            📢 <b>{current.First().ChannelPost.Channel.Link}</b>

            {string.Join("\n\n", current.Select(p => p.ChannelPost.Text))}
            """;

        var buttons = new List<InlineKeyboardButton>();
        if (page > 0)
            buttons.Add(InlineKeyboardButton.WithCallbackData("◀ Назад", $"/groupedposts:{page - 1}"));
        if (page < grouped.Count - 1)
            buttons.Add(InlineKeyboardButton.WithCallbackData("▶ Далее", $"/groupedposts:{page + 1}"));

        var markup = new InlineKeyboardMarkup(buttons);

        await telegramBotClient.ReactivelySendAsync(userId, text, replyMarkup: markup);

        if (page == grouped.Count - 1)
        {
            await delayedRepo.RemoveRangeAsync(posts.Select(u => u.Id));
            await unitOfWork.CommitAsync();
        }
    }

    private async Task ScheduleNotificationJobIfNeeded(UserEn user)
    {
        var delayedRepo = unitOfWork.Repository<long, DelayedUserPost>();
        var hasPosts = await delayedRepo.GetIQueryable()
            .AnyAsync(p => p.UserId == user.Id);

        if (!hasPosts) return;

        var tzId = user.ChannelUserSettings.TimeZoneId ?? "UTC";
        var tz = TimeZoneInfo.FindSystemTimeZoneById(tzId);

        var nowUserTz = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
        var notifTime = nowUserTz.Date + user.ChannelUserSettings.NotificationTime.Value.ToTimeSpan();

        if (notifTime <= nowUserTz)
            notifTime = notifTime.AddDays(1);

        var notifUtc = TimeZoneInfo.ConvertTimeToUtc(notifTime, tz);

        BackgroundJob.Schedule<TelegramSenderService>(
            s => s.SendGroupedPostsAsync(user.Id, 0),
            notifUtc - DateTime.UtcNow);
    }
}