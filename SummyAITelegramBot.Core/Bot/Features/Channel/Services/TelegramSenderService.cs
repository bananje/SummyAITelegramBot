using Hangfire;
using Hangfire.Common;
using Microsoft.EntityFrameworkCore;
using SummyAITelegramBot.Core.Abstractions;
using SummyAITelegramBot.Core.AI.Abstractions;
using SummyAITelegramBot.Core.Bot.Extensions;
using SummyAITelegramBot.Core.Bot.Features.Channel.Abstractions;
using SummyAITelegramBot.Core.Domain.Enums;
using SummyAITelegramBot.Core.Domain.Models;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using UserEn = SummyAITelegramBot.Core.Domain.Models.User;

namespace SummyAITelegramBot.Core.Bot.Features.Channel.Services;

public class TelegramSenderService(
    ITelegramBotClient telegramBotClient,
    ISummarizationStrategyFactory aiFactory,
    IStaticImageService imageService,
    IRecurringJobManager recurringJobManager,
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
        //var sentPostsRepo = unitOfWork.Repository<int, SentUserPost>();

        foreach (var user in users)
        {
            var settings = user.ChannelUserSettings ?? throw new Exception($"Отсутсвуют настройки у пользователя ID:{user.Id}");

            var delaydedPosts = await delayedRepo.GetIQueryable()
                    .Where(p => p.UserId == user.Id && !p.IsSend)
                    .Include(p => p.ChannelPost)
                    .ToListAsync();

            if (delaydedPosts.Count > 0)
            {
                var allTexts = new List<string>(delaydedPosts.Select(p => p.ChannelPost.Text));
                var currentPostText = post.Text;
                var message = string.Join("\n\n", allTexts);

                var aiHandler = aiFactory.Create(AiModel.TextHeader);
                var isUniquePost = await aiHandler.ValidateOfUniqueTextAsync(message, currentPostText);

                if (isUniquePost)
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

            #region InstantlyNotify
            //    if (isUniquePost)
            //    {
            //        var messageText = FormatInstantSummary(post);

            //        if (post.MediaPath is not null)
            //        {
            //            await using var stream = imageService.GetImageStream(post.MediaPath, "media_cache");

            //            await telegramBotClient.SendPhoto(
            //                chatId: user.Id,
            //                photo: new InputFileStream(stream),
            //                caption: messageText,
            //                parseMode: ParseMode.Html);
            //        }
            //        else
            //        {
            //            await telegramBotClient.SendMessage(
            //                chatId: user.Id,
            //                messageText,
            //                parseMode: ParseMode.Html,
            //                linkPreviewOptions: true);
            //        }


            //        if (settings.InstantlyTimeNotification.GetValueOrDefault())
            //    {
            //        // Получим посты за последние 10 минут
            //        var recentPosts = await sentPostsRepo.GetIQueryable()
            //            .Where(p => p.UserId == user.Id &&
            //                        p.SentAt >= DateTime.UtcNow.AddMinutes(-10))
            //            .Include(p => p.ChannelPost)
            //            .ToListAsync();

            //        if (recentPosts.Count == 0)
            //        {
            //            var messageText = FormatInstantSummary(post);

            //            if (post.MediaPath is not null)
            //            {
            //                await using var stream = imageService.GetImageStream(post.MediaPath, "media_cache");

            //                await telegramBotClient.SendPhoto(
            //                    chatId: user.Id,
            //                    photo: new InputFileStream(stream),
            //                    caption: messageText,
            //                    parseMode: ParseMode.Html);
            //            }
            //            else
            //            {
            //                await telegramBotClient.SendMessage(
            //                    chatId: user.Id,
            //                    messageText,
            //                    parseMode: ParseMode.Html,
            //                    linkPreviewOptions: true);
            //            }


            //            await sentPostsRepo.AddAsync(new SentUserPost
            //            {
            //                UserId = user.Id,
            //                ChannelId = post.ChannelId,
            //                ChannelPostId = post.Id,
            //                SentAt = DateTime.UtcNow
            //            });
            //            await unitOfWork.CommitAsync();
            //            return;
            //        }

            //        // Объединяем все тексты
            //        var allTexts = new List<string>(recentPosts.Select(p => p.ChannelPost.Text));
            //        var currentPostText = post.Text;
            //        var message = string.Join("\n\n", allTexts);

            //        var aiHandler = aiFactory.Create(AiModel.TextHeader);
            //        var isUniquePost = await aiHandler.ValidateOfUniqueTextAsync(message, currentPostText);

            //        if (isUniquePost)
            //        {
            //            var messageText = FormatInstantSummary(post);

            //            if (post.MediaPath is not null)
            //            {
            //                await using var stream = imageService.GetImageStream(post.MediaPath, "media_cache");

            //                await telegramBotClient.SendPhoto(
            //                    chatId: user.Id,
            //                    photo: new InputFileStream(stream),
            //                    caption: messageText,
            //                    parseMode: ParseMode.Html);
            //            }
            //            else
            //            {
            //                await telegramBotClient.SendMessage(
            //                    chatId: user.Id,
            //                    messageText,
            //                    parseMode: ParseMode.Html,
            //                    linkPreviewOptions: true);
            //            }

            //            await sentPostsRepo.AddAsync(new SentUserPost
            //            {
            //                UserId = user.Id,
            //                ChannelId = post.ChannelId,
            //                ChannelPostId = post.Id,
            //                SentAt = DateTime.UtcNow
            //            });
            //        }
            //    }
            //    else
            //    {
            //        await delayedRepo.AddAsync(new DelayedUserPost
            //        {
            //            UserId = user.Id,
            //            ChannelId = post.ChannelId,
            //            CreatedDate = DateTime.UtcNow,
            //            ChannelPostId = post.Id
            //        });
            //    }
            //}
            #endregion

            await unitOfWork.CommitAsync();
        }
    }

    public async Task SendSubscriptionOffersToEligibleUsersAsync()
    {
        var userRepository = unitOfWork.Repository<long, UserEn>();
        var targetDate = DateTime.UtcNow.Date.AddDays(2);

        var users = await userRepository.GetIQueryable()
            .Where(u =>
                u.Subscription != null &&
                u.Subscription.Type == SubscriptionType.TrialSubscription &&
                u.Subscription.EndDate.Date == targetDate)
            .ToListAsync();

        foreach (var user in users)
        {
            await SendSubscriptionOfferAsync(user);
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

            string? postLink = !string.IsNullOrEmpty(channelUsername)
                ? $"https://t.me/{channelUsername}/{post.Id}"
                : null;

            sb.AppendLine($"• {postText}");
            if (postLink != null)
                sb.AppendLine($"<a href=\"{postLink}\">🔍 Подробнее</a>");
            sb.AppendLine();
        }

        // Первая строка: индикатор страницы
        var pageIndicatorRow = new[]
        {
            InlineKeyboardButton.WithCallbackData($"{page + 1}/{grouped.Count}", "noop")
        };

        // Вторая строка: кнопки навигации
        var navRow = new List<InlineKeyboardButton>();
        if (page > 0)
            navRow.Add(InlineKeyboardButton.WithCallbackData("◀", $"/groupedposts:{page - 1}"));
        if (page < grouped.Count - 1)
            navRow.Add(InlineKeyboardButton.WithCallbackData("▶", $"/groupedposts:{page + 1}"));

        var markup = new InlineKeyboardMarkup(new[]
        {
            pageIndicatorRow,
            navRow.ToArray()
        });

        await telegramBotClient.ReactivelySendAsync(
            chatId: userId,
            text: sb.ToString(),
            replyMarkup: markup
        );

        posts.ForEach(post => { post.IsSend = true; });

        await delayedRepo.UpdateRangeAsync(posts);

        await unitOfWork.CommitAsync();
    }

    private async Task SendSubscriptionOfferAsync(UserEn user)
    {
        var keyboardButtons = new List<List<InlineKeyboardButton>>
        {
            new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData("199р/меc", "/pay"),
                InlineKeyboardButton.WithCallbackData("1500р/навсегда", "/pay")
            },
            new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData("✅ Продолжить бесплатно", "/complete")
            }
        };

        var text = $"""
                До окончания пробного периода остаётся 2 дня🤝
                Summy хочет проявить заботу и за небольшое вознаграждение присылать вам сводки безлимитно

                <b> *Вы можете остаться на бесплатной версии, где можно добавить до 3-х каналов❤️</b>
            """;

        var stream = imageService.GetImageStream("summy_sub.jpg");

        await telegramBotClient.ReactivelySendPhotoAsync(
            user.ChatId,
            photo: stream,
            caption: text,
            replyMarkup: new InlineKeyboardMarkup(keyboardButtons)
        );
    }

    private string FormatInstantSummary(ChannelPost post)
    {
        var channelTitle = post.Channel?.Title?.Trim() ?? "Канал";
        var postText = string.IsNullOrWhiteSpace(post.Text) ? "Нет текста" : post.Text.Trim();
        var channelUrlRaw = post.Channel?.Link?.Trim();

        // Очистка лишних пустых строк внутри текста
        postText = string.Join("\n", postText
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim()));

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