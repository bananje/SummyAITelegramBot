using Microsoft.EntityFrameworkCore;
using Serilog;
using SummyAITelegramBot.Core.Abstractions;
using SummyAITelegramBot.Core.Bot.Abstractions;
using SummyAITelegramBot.Core.Bot.Attributes;
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

                await settingsRepo.UpdateAsync(userSettings);
                await unitOfWork.CommitAsync();
                await updateFactory.DispatchAsync(update, "/shownotificationtimesettings");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to set timezone for user {UserId}", userId);

                await updateFactory.DispatchAsync(update, "/showtimezonesettings");
            }
        }
    }
}
