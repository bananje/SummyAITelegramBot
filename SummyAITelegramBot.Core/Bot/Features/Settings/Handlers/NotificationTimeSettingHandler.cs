using Telegram.Bot.Types;
using Telegram.Bot;
using SummyAITelegramBot.Core.Bot.Abstractions;
using SummyAITelegramBot.Core.Abstractions;
using SummyAITelegramBot.Core.Domain.Models;
using SummyAITelegramBot.Core.Bot.Attributes;
using SummyAITelegramBot.Core.Bot.Utils;
using Microsoft.EntityFrameworkCore;

namespace SummyAITelegramBot.Core.Bot.Features.Settings.Handlers;

[TelegramUpdateHandler(Consts.NotificationTimeSettingCallBackPrefix)]
public class NotificationTimeSettingHandler(
    ITelegramBotClient bot,
    ITelegramUpdateFactory telegramUpdateFactory,
    IUnitOfWork unitOfWork) : ITelegramUpdateHandler
{
    public async Task HandleAsync(Update update)
    {
        var query = update.CallbackQuery;

        if (query.Data != null && query.Data.StartsWith(Consts.NotificationTimeSettingCallBackPrefix))
        {
            var settingsRepostitory = unitOfWork.Repository<Guid, ChannelUserSettings>();
            var timeStr = query.Data.Substring(Consts.NotificationTimeSettingCallBackPrefix.Length);

            var userSettings = await settingsRepostitory.GetIQueryable()
                .FirstOrDefaultAsync(us => us.UserId == query.Message.Chat.Id);

            if (timeStr == "realtime")
            {
                userSettings.InstantlyTimeNotification = true;
            }
            else if (TimeOnly.TryParse(timeStr, out var time))
            {
                userSettings.NotificationTime = time;
                userSettings.InstantlyTimeNotification = false;
            }
            else
            {
                await telegramUpdateFactory.DispatchAsync(update, "/shownotificationtimesettings");
                return;
            }

            await unitOfWork.CommitAsync();
            await telegramUpdateFactory.DispatchAsync(update, "/complete");
        }
    }
}