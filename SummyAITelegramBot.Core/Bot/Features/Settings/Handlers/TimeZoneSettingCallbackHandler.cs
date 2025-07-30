using Hangfire;
using Hangfire.Common;
using Microsoft.EntityFrameworkCore;
using Serilog;
using SummyAITelegramBot.Core.Abstractions;
using SummyAITelegramBot.Core.Bot.Abstractions;
using SummyAITelegramBot.Core.Bot.Attributes;
using SummyAITelegramBot.Core.Bot.Features.Channel.Services;
using SummyAITelegramBot.Core.Bot.Utils;
using SummyAITelegramBot.Core.Domain.Models;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace SummyAITelegramBot.Core.Bot.Features.Settings.Handlers;

[TelegramUpdateHandler(Consts.TimeZoneSettingCallBackPrefix)]
public class TimeZoneSettingCallbackHandler(
    IUnitOfWork unitOfWork,
    ITelegramBotClient bot,
    ITelegramUpdateFactory updateFactory,
    IRecurringJobManager recurringJobManager,
    ILogger logger) : ITelegramUpdateHandler
{
    public async Task HandleAsync(Update update)
    {
        var query = update.CallbackQuery;

        if (query.Data != null && query.Data.StartsWith(Consts.TimeZoneSettingCallBackPrefix))
        {
            var userId = query.Message.Chat.Id;
            var timezoneId = query.Data.Substring(Consts.TimeZoneSettingCallBackPrefix.Length);

            // Проверим, что это валидная тайм-зона (для надежности)
            try
            {
                var settingsRepo = unitOfWork.Repository<Guid, ChannelUserSettings>();

                var userSettings = await settingsRepo.GetIQueryable()
                    .FirstOrDefaultAsync(us => us.UserId == userId);

                if (userSettings is null)
                {
                    userSettings = new ChannelUserSettings()
                    {
                        UserId = userId                      
                    };
                }

                var tz = TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
                userSettings.TimeZoneId = tz.Id;

                await settingsRepo.CreateOrUpdateAsync(userSettings);
                await unitOfWork.CommitAsync();

                ScheduleRecurringJob(userSettings);

                await updateFactory.DispatchAsync(update, "/complete");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to set timezone for user {UserId}", userId);

                await updateFactory.DispatchAsync(update, "/showtimezonesettings");
            }
        }
    }

    private void ScheduleRecurringJob(ChannelUserSettings settings)
    {
        if (settings?.NotificationTime == null)
            return;

        var timeZoneId = settings.TimeZoneId ?? "UTC";
        TimeZoneInfo timeZone;

        try
        {
            timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch
        {
            timeZone = TimeZoneInfo.Utc;
        }

        var notificationTime = settings.NotificationTime.Value;
        //var cronExpression = Cron.Daily(notificationTime.Hour, notificationTime.Minute);
        var cronExpression = Cron.Daily(17, 37);
        var recurringJobId = $"SendGroupedPosts_User_{settings.UserId}";
        try
        {
            recurringJobManager.AddOrUpdate(
                recurringJobId,
                Job.FromExpression<TelegramSenderService>(service => service.SendGroupedPostsAsync(settings.UserId, 0)),
                cronExpression,
                timeZone
            );
        }
        catch (Exception ex)
        {
            // логируй ошибку
            logger.Error($"[Hangfire AddOrUpdate Error] {ex}");
        }
    }
}
