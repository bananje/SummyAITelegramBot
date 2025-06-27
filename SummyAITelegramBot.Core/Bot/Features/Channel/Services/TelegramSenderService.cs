using Hangfire;
using Microsoft.EntityFrameworkCore;
using SummyAITelegramBot.Core.Abstractions;
using SummyAITelegramBot.Core.Bot.Features.Channel.Abstractions;
using SummyAITelegramBot.Core.Domain.Models;
using Telegram.Bot;
using UserEn = SummyAITelegramBot.Core.Domain.Models.User;

namespace SummyAITelegramBot.Core.Bot.Features.Channel.Services;

public class TelegramSenderService(
    ITelegramBotClient telegramBotClient,
    IRepository<long, UserEn> userRepository) : ITelegramSenderService
{
    public async Task ResolveNotifyUsersAsync(ChannelPost post)
    {
        var users = await userRepository.GetIQueryable()
            .Where(u => u.Channels.Any(c => c.Id == post.ChannelId))
            .Include(u => u.ChannelUserSettings)
            .ToListAsync();

        foreach (var user in users)
        {
            var userSettings = user.ChannelUserSettings;

            if (userSettings.InstantlyTimeNotification.GetValueOrDefault())
            {
                await telegramBotClient.SendMessage(chatId: user.Id, text: "push" + post.Text);
            }
            else
            {
                if (string.IsNullOrEmpty(userSettings.TimeZoneId))
                {
                    // Если тайм-зона не указана — можно обработать по дефолту (например, UTC)
                    userSettings.TimeZoneId = "UTC";
                }

                var tz = TimeZoneInfo.FindSystemTimeZoneById(userSettings.TimeZoneId);

                var nowUserTz = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
                var todayTargetUserTz = nowUserTz.Date + userSettings.NotificationTime.Value.ToTimeSpan();

                if (todayTargetUserTz <= nowUserTz)
                {
                    // если время уже прошло в часовом поясе пользователя — на следующий день
                    todayTargetUserTz = todayTargetUserTz.AddDays(1);
                }

                // переведём обратно в UTC для Hangfire
                var todayTargetUtc = TimeZoneInfo.ConvertTimeToUtc(todayTargetUserTz, tz);

                BackgroundJob.Schedule<TelegramSenderService>(
                    job => job.NotifyUserAsync(post.Text, user.Id),
                    todayTargetUtc - DateTime.UtcNow);
            }
        }
    }

    public async Task NotifyUserAsync(string text, long chatId)
    {
        await telegramBotClient.SendMessage(chatId, $"(отложено) {text}");
    }
}