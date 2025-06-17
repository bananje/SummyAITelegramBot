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
            .Include(u => u.UserSettings)
            .ToListAsync();

        foreach (var user in users)
        {
            var userSettings = user.UserSettings.FirstOrDefault(u => u.ChannelId == post.ChannelId)
                ?? throw new Exception($"User with id: {user.Id} does not have settings");

            if (userSettings.InstantlyTimeNotification)
            {
                await telegramBotClient.SendMessage(chatId: user.ChatId, text: "push" + post.Text);
            }
            else
            {
                var todayTarget = DateTime.UtcNow + userSettings.NotificationTime.Value.ToTimeSpan();

                if (todayTarget <= DateTime.UtcNow)
                {
                    // Если уже прошло — можно либо пропустить, либо перенести на завтра
                    todayTarget = todayTarget.AddDays(1);
                }

                BackgroundJob.Schedule<TelegramSenderService>(
                    job => job.NotifyUserAsync(post.Text, user.ChatId),
                    delay: todayTarget - DateTime.UtcNow);
            }
        }
    }

    private async Task NotifyUserAsync(string text, long chatId)
    {
        await telegramBotClient.SendMessage(chatId, $"(отложено) {text}");
    }
}