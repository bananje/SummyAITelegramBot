using FluentResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SummyAITelegramBot.Core.Abstractions;
using SummyAITelegramBot.Core.Bot.Abstractions;
using SummyAITelegramBot.Core.Bot.Extensions;
using SummyAITelegramBot.Core.Domain.Models;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace SummyAITelegramBot.Core.Bot.Features.Channel.Handlers;

public class SettingsConfigurationHandler(
    ITelegramBotClient bot,
    IUnitOfWork unitOfWork,
    IStaticImageService imageService,
    IMemoryCache cache,
    string chainCachePrefix) : IStepOnChainHandler<UserSettings>
{
    public IStepOnChainHandler<UserSettings>? Next { get; set; }

    private static readonly string CallbackPrefix = "add:channel_chain_settings_config:";    

    public async Task<Result> HandleAsync(Update update, UserSettings userSettings)
    {
        var query = update.CallbackQuery;

        if (query.Data != null && query.Data.StartsWith(CallbackPrefix))
        {
            var settingsRepostitory = unitOfWork.Repository<Guid, UserSettings>();
            var channelRepository = unitOfWork.Repository<long, Domain.Models.Channel>();
            var settingCommand = query.Data.Substring(CallbackPrefix.Length);

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


                cache.Remove(chainCachePrefix);

                // TO:DO продумать куда будет переход
                return Result.Fail("");
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

    public async Task ShowStepAsync(Update update)
    {
        var userId = update.Message.From!.Id;
        var userRepository = unitOfWork.Repository<long, Domain.Models.User>();

        var user = await userRepository.GetIQueryable()
            .Include(u => u.UserSettings)
            .FirstOrDefaultAsync(u => u.Id == userId) 
                ?? throw new Exception($"Ошибка при настройке пользователя {userId}.");

        var keyboard = new List<List<InlineKeyboardButton>>
        {
            new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData("Персонально настроить", $"{CallbackPrefix}personal")
            }
        };

        var hasGlobalSetting = user.UserSettings.Any(u => u.IsGlobal);

        if (hasGlobalSetting)
        {
            keyboard.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData("Установить как у всех каналов", $"{CallbackPrefix}global_apply"),
                InlineKeyboardButton.WithCallbackData("Сбросить общие настройки", $"{CallbackPrefix}global_clear")
            });
        }
        else
        {
            keyboard.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData("Настроить сразу для всех каналов", $"{CallbackPrefix}global_create"),
            });
        }

        var text = $"""
                2️⃣ <b>Указываем время получения сводок</b>

                {update.Message.From.FirstName}, пожалуйста, добавьте удобное
                для Вас день и время получения сводок🦉
                """;

        await using var stream = imageService.GetImageStream("add_channel.jpg");

        await bot.SendOrEditMessageAsync(
                cache,
                update,
                photo: stream,
                caption: text,
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                replyMarkup: new InlineKeyboardMarkup(keyboard)
        );
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