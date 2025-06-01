using Telegram.Bot.Types;
using Telegram.Bot;
using SummyAITelegramBot.Core.Bot.Abstractions;
using SummyAITelegramBot.Core.Bot.Features.Settings.Handlers;
using SummyAITelegramBot.Core.Bot.Attributes;
using SummyAITelegramBot.Core.Domain.Models;
using Microsoft.Extensions.Caching.Memory;
using SummyAITelegramBot.Core.Abstractions;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot.Types.ReplyMarkups;
using UserEn = SummyAITelegramBot.Core.Domain.Models.User;
using Serilog;

namespace SummyAITelegramBot.Core.Bot.Features.Settings;

[CallbackHandler("settings")]
public class SettingsChainOfStepsHandler(
    ITelegramBotClient bot,
    IMemoryCache cache,
    IStaticImageService imageService,
    IRepository<Guid, UserSettings> settingsRepository,
    IRepository<long, UserEn> userRepository,
    ILogger logger) : ICallbackHandler
{
    private const string ChainCachePrefix = "settings_chain_";
    private const string UserSettingsCachePrefix = "user_settings_";

    public async Task HandleAsync(CallbackQuery query)
    {
        var userId = query.From.Id;
        var chatId = query.Message.Chat.Id;

        if (!await userRepository.GetIQueryable().AnyAsync(u => u.Id == userId))
        {
            var keyboard = new ReplyKeyboardMarkup(new[]
            {
                new KeyboardButton[] { "/start" },
            });

            logger.Error($"User with ID:{userId} not found!");
            await bot.SendMessage(chatId, "Кажется я сломалась! Перезапусти меня через /start.", replyMarkup: keyboard);
        }

        var chainKey = $"{ChainCachePrefix}{chatId}";
        var userKey = $"{UserSettingsCachePrefix}{userId}";        

        if (!cache.TryGetValue<IChainOfStepsHandler<UserSettings>>(chainKey, out var handler))
            return;

        cache.TryGetValue<UserSettings>(userKey, out var userSettings);

        if (userSettings is null)
        {
            userSettings = await settingsRepository
                .GetIQueryable()
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (userSettings is null)
            {
                var keyboard = new InlineKeyboardMarkup(new[]
                {
                    InlineKeyboardButton.WithCallbackData("/settings", "")
                });

                await bot.SendMessage(chatId, "Ошибка заполнения настроек! Попробуй снова.", replyMarkup: keyboard);
            }
        }

        await handler.HandleAsync(bot, query, userSettings);

        if (handler.Next != null)
        {
            cache.Set(chainKey, handler.Next, TimeSpan.FromMinutes(5));
        }
        else
        {
            cache.Remove(chainKey);
            await settingsRepository.UpdateAsync(userSettings!);
        }
    }

    public async Task StartChainAsync(long chatId, long userId)
    {
        var chainKey = $"{ChainCachePrefix}{chatId}";

        // Если цепочка уже существует — показать текущий шаг заново
        if (cache.TryGetValue<IChainOfStepsHandler<UserSettings>>(chainKey, out var existingHandler))
        {
            await existingHandler.ShowStepAsync(bot, chatId);
            return;
        }

        var userSettings = await settingsRepository
            .GetIQueryable()
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (userSettings is null)
        {
            userSettings = new UserSettings { UserId = userId };
        }

        cache.Set($"{UserSettingsCachePrefix}{userId}", userSettings, TimeSpan.FromMinutes(5));


        var notification = new NotificationsSettingsHandler();
        var media = new MediaStepHandler(imageService);
        var stop = new ChannelReductionStepHandler();

        notification.Next = media;
        media.Next = stop;
         
        cache.Set(chainKey, notification, TimeSpan.FromMinutes(10));

        await notification.ShowStepAsync(bot, chatId);
    }
}