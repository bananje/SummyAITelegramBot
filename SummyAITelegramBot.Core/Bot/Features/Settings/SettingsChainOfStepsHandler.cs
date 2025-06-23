using Telegram.Bot.Types;
using Telegram.Bot;
using SummyAITelegramBot.Core.Bot.Abstractions;
using SummyAITelegramBot.Core.Domain.Models;
using Microsoft.Extensions.Caching.Memory;
using SummyAITelegramBot.Core.Abstractions;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot.Types.ReplyMarkups;
using UserEn = SummyAITelegramBot.Core.Domain.Models.User;
using Serilog;

namespace SummyAITelegramBot.Core.Bot.Features.Settings;

//public class SettingsChainOfStepsHandler(
//    ITelegramBotClient bot,
//    IMemoryCache cache,
//    IStaticImageService imageService,
//    IRepository<Guid, UserSettings> settingsRepository,
//    IRepository<long, UserEn> userRepository,
//    ILogger logger) 
//{
//    private const string ChainCachePrefix = "settings_chain_";
//    private const string UserSettingsCachePrefix = "user_settings_";

//    public async Task HandleAsync(Update query)
//    {
//        var userId = query.From.Id;
//        var chatId = query.Chat.Id;

//        var user = await userRepository.GetByIdAsync(userId)
//            ?? throw new Exception($"Ошибка при настройке пользователя {userId}.");

//        var chainKey = $"{ChainCachePrefix}{chatId}";
//        var userKey = $"{UserSettingsCachePrefix}{userId}";        

//        if (!cache.TryGetValue<IStepOnChainHandler>(chainKey, out var handler))
//        {
//            await SendErrorSettingsMessage(chatId, userId);
//            return;
//        }          

//        cache.TryGetValue<UserSettings>(userKey, out var userSettings);

//        if (userSettings is null)
//        {
//            userSettings = await settingsRepository
//                .GetIQueryable()
//                .FirstOrDefaultAsync(u => u.UserId == userId);

//            if (userSettings is null)
//            {
//                var keyboard = new InlineKeyboardMarkup(new[]
//                {
//                    InlineKeyboardButton.WithCallbackData("/settings", "/settings")
//                });

//                await bot.SendMessage(chatId, "Ошибка заполнения настроек! Попробуй снова.", replyMarkup: keyboard);
//            }
//        }

//        await handler.HandleAsync(query);

//        if (handler.Next != null)
//        {
//            cache.Set(chainKey, handler.Next, TimeSpan.FromMinutes(5));
//        }
//        else
//        {
//            cache.Remove(chainKey);
//            await settingsRepository.UpdateAsync(userSettings!);
//        }
//    }

//    public async Task StartChainAsync(Message message)
//    {
//        var chatId = message.Chat.Id;
//        var userId = message.From.Id;
//        var chainKey = $"{ChainCachePrefix}{chatId}";

//        // Если цепочка уже существует — показать текущий шаг заново
//        if (cache.TryGetValue<IStepOnChainHandler>(chainKey, out var existingHandler))
//        {
//            await existingHandler.ShowStepAsync(message);
//            return;
//        }

//        //var userSettings = await settingsRepository
//        //    .GetIQueryable()
//        //    .FirstOrDefaultAsync(u => u.UserId == userId);

//        //if (userSettings is null)
//        //{
//        //    userSettings = new UserSettings { UserId = userId };
//        //}

//        //cache.Set($"{UserSettingsCachePrefix}{userId}", userSettings, TimeSpan.FromMinutes(5));

//        //var notification = new NotificationsSettingsHandler();
//        //var media = new MediaStepHandler(imageService);
//        //var reduction = new ChannelReductionStepHandler();
//        //var globalSetting = new GlobalSettingsStepHadler();
//        //var finish = new FinishSettingsStepHandler();

//        //notification.Next = media;
//        //media.Next = reduction;
//        //reduction.Next = globalSetting;
//        //globalSetting.Next = finish;

//        //cache.Set(chainKey, notification, TimeSpan.FromMinutes(10));

//        //await notification.ShowStepAsync(bot, chatId);
//    }

//    private async Task SendErrorSettingsMessage(long chatId, long userId)
//    {
//        var keyboard = new InlineKeyboardMarkup(new[]
//            {
//                InlineKeyboardButton.WithCallbackData("⚙️Настройки", "/settings"),
//                InlineKeyboardButton.WithCallbackData("⚙️Стоп", "/settings"),
//            });

//        logger.Error($"User with ID:{userId} not found!");
//        await bot.SendMessage(chatId, "Кажется, сбились настройки😕. Давай попробуем ещё раз!", replyMarkup: keyboard);
//    }
//}