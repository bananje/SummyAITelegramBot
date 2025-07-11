using Microsoft.EntityFrameworkCore;
using SummyAITelegramBot.Core.Abstractions;
using SummyAITelegramBot.Core.Bot.Abstractions;
using SummyAITelegramBot.Core.Bot.Attributes;
using SummyAITelegramBot.Core.Bot.Utils;
using SummyAITelegramBot.Core.Domain.Models;
using Telegram.Bot.Types;

namespace SummyAITelegramBot.Core.Bot.Features.Settings.Handlers;

[TelegramUpdateHandler(Consts.ChannelSettingsCallbackPrefix)]
public class SettingsConfigurationDispatcherHandler(
    IUnitOfWork unitOfWork,
    ITelegramUpdateFactory telegramUpdateFactory) : ITelegramUpdateHandler
{
    public async Task HandleAsync(Update update)
    {
        var query = update.CallbackQuery;

        if (query.Data != null && query.Data.StartsWith(Consts.ChannelSettingsCallbackPrefix))
        {
            var settingsRepostitory = unitOfWork.Repository<Guid, ChannelUserSettings>();
            var channelRepository = unitOfWork.Repository<long, Domain.Models.Channel>();
            var settingCommand = query.Data.Substring(Consts.ChannelSettingsCallbackPrefix.Length);

            if (settingCommand == "apply")
            {
                await telegramUpdateFactory.DispatchAsync(update, "/complete");
                return;
            }

            if (settingCommand == "clear-create")
            {
                var userSettings = await settingsRepostitory
                    .GetIQueryable()
                    .FirstOrDefaultAsync(u => u.UserId == query.Message.Chat.Id);

                await settingsRepostitory.RemoveAsync(userSettings.Id);
                await unitOfWork.CommitAsync();
            }

            await telegramUpdateFactory.DispatchAsync(update, "/shownotificationtimesettings");
            return;
        }
    }
}