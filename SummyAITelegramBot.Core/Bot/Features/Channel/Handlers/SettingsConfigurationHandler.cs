using FluentResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SummyAITelegramBot.Core.Abstractions;
using SummyAITelegramBot.Core.Bot.Abstractions;
using SummyAITelegramBot.Core.Bot.Attributes;
using SummyAITelegramBot.Core.Bot.Utils;
using SummyAITelegramBot.Core.Domain.Models;
using Telegram.Bot;
using Telegram.Bot.Types;
using TL;

namespace SummyAITelegramBot.Core.Bot.Features.Channel.Handlers;

[TelegramUpdateHandler(Consts.ChannelSettingsCallbackPrefix)]
public class SettingsConfigurationHandler(
    ITelegramBotClient bot,
    IUnitOfWork unitOfWork,
    IStaticImageService imageService,
    IMemoryCache cache,
    string chainCachePrefix) : ITelegramUpdateHandler
{
    
    public async Task HandleAsync(Update update)
    {
        var query = update.CallbackQuery;

        if (query.Data != null && query.Data.StartsWith(Consts.ChannelSettingsCallbackPrefix))
        {
            var settingsRepostitory = unitOfWork.Repository<Guid, UserSettings>();
            var channelRepository = unitOfWork.Repository<long, Domain.Models.Channel>();
            var settingCommand = query.Data.Substring(Consts.ChannelSettingsCallbackPrefix.Length);

            cache.TryGetValue<UserSettings>($"{Consts.UserSettingsCachePrefix}{query.From.Id}", out var userSettings);

            if (settingCommand == "global_apply")
            {
                var globalUserSettings = await settingsRepostitory.GetIQueryable()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.User.Id == query.From.Id);

                userSettings.Day = globalUserSettings.Day;

                if (globalUserSettings.InstantlyTimeNotification) 
                {
                    userSettings.InstantlyTimeNotification = globalUserSettings.InstantlyTimeNotification;
                }

                if (globalUserSettings.NotificationTime is not null)
                {
                    userSettings.NotificationTime = globalUserSettings.NotificationTime;
                }

                cache.Set($"{SettingsCachePrefix}{chatId}", userSettings, TimeSpan.FromMinutes(5));
            }

            if (settingCommand == "global_clear")
            {
                await ResetGlobalUserSettings(settingsRepostitory);

                await ShowStepAsync(update);
            }

            if (settingCommand == "global_create")
            {
                await ResetGlobalUserSettings(settingsRepostitory);
                userSettings.IsGlobal = true;
            }

            if (Next != null)
                await Next.ShowStepAsync(update);

            return Result.Ok();
        }

        return Result.Fail("");
    }

    private async Task ResetGlobalUserSettings(IRepository<Guid, UserSettings> repository)
    {
        var globalUserSetting = await repository.GetIQueryable()
                    .FirstOrDefaultAsync(u => u.IsGlobal);

        if (globalUserSetting is not null)
        {
            await repository.RemoveAsync(globalUserSetting.Id);
            await unitOfWork.CommitAsync();
        }
    } 
}