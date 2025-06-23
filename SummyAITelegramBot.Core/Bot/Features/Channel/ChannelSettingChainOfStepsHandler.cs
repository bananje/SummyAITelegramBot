using Microsoft.Extensions.Caching.Memory;
using Serilog;
using SummyAITelegramBot.Core.Abstractions;
using SummyAITelegramBot.Core.Bot.Abstractions;
using SummyAITelegramBot.Core.Bot.Attributes;
using SummyAITelegramBot.Core.Bot.Features.Channel.Abstractions;
using SummyAITelegramBot.Core.Bot.Features.Channel.Handlers;
using SummyAITelegramBot.Core.Bot.Features.Settings.Handlers;
using SummyAITelegramBot.Core.Domain.Models;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace SummyAITelegramBot.Core.Bot.Features.Channel;

[TelegramUpdateHandler("add", true)]
public class ChannelSettingChainOfStepsHandler(
    ITelegramBotClient bot,
    ITelegramChannelAdapter telegramChannelAdapter,
    IMemoryCache cache,
     IStaticImageService imageService,
    IUnitOfWork unitOfWork,
    ILogger logger) : ITelegramUpdateHandler
{
    private const string ChainCachePrefix = "add_channel_chain_";
    private const string SettingsCachePrefix = "user_settings:";

    public async Task HandleAsync(Update update)
    {
        var message = update.Message is null ?
            update.CallbackQuery.Message : update.Message;
        var userId = message.Chat.Id;
        var chainKey = $"{ChainCachePrefix}{userId}";
        var userSettingsCacheKey = $"{SettingsCachePrefix}{userId}";
        string cacheMessageKey = $"EditMessage:{message.Chat.Id}:{userId}";

        try
        {
            var settingRepo = unitOfWork.Repository<Guid, UserSettings>();

            if (!cache.TryGetValue<IStepOnChainHandler<UserSettings>>(chainKey, out var handler))
            {
                await StartChainAsync(update);
                return;
            }

            if (!cache.TryGetValue<UserSettings>(userSettingsCacheKey, out var userSettings))
            {
                await StartChainAsync(update);
                return;
            }

            if (message.Text == "/add" &&
                cache.TryGetValue<IStepOnChainHandler<UserSettings>>(chainKey, out var existingHandler))
            {
                await existingHandler.ShowStepAsync(update);
                return;
            }

            var result = await handler.HandleAsync(update, userSettings);

            // выйти из цепочки
            if (result.IsFailed)
            {
                cache.Remove(chainKey);
                cache.Remove(userSettingsCacheKey);
            }
            // какой-то косяк при выполнении, юзер повторяет шаг
            else if (result.Reasons.Count != 0)
            {
                return;
            }

            if (handler.Next != null)
            {
                cache.Set(chainKey, handler.Next, TimeSpan.FromMinutes(2));
            }
            else
            {
                cache.Remove(chainKey);
                cache.Remove(userSettingsCacheKey);

                await settingRepo.CreateOrUpdateAsync(userSettings);
                await unitOfWork.CommitAsync();
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, ex.Message);
            cache.Remove(chainKey);
            cache.Remove(userSettingsCacheKey);

            cache.TryGetValue<int?>(cacheMessageKey, out int? msgId);
            
            if (msgId == 0) { msgId = message.MessageId; }

            if (msgId != 0 && msgId is not null)
            {
                await bot.DeleteMessage(message.Chat.Id, msgId.Value);
            }
        }  
    }

    public async Task StartChainAsync(Update update)
    {
        var message = update.Message is null ?
            update.CallbackQuery.Message : update.Message;

        var chatId = message.Chat.Id;
        var chainKey = $"{ChainCachePrefix}{chatId}";

        var addChannelHandler = new AddChannelHandler(
            unitOfWork, 
            bot, 
            imageService,
            telegramChannelAdapter,
            cache);
        var settingsConfHandler = new SettingsConfigurationHandler(
            bot,
            unitOfWork,
            imageService,
            cache,
            chainKey);
        var notificationTimeSettingHandler = new NotificationTimeSettingHandler(
            bot,
            unitOfWork);
        var notificationDaySettingHandler = new NotificationDaySettingHandler(
            bot,
            unitOfWork,
            cache);
        var finish = new FinishChannelAddingHandler(
            bot,
            unitOfWork,
            imageService,
            cache);

        addChannelHandler.Next = settingsConfHandler;
        settingsConfHandler.Next = notificationDaySettingHandler;
        notificationDaySettingHandler.Next = notificationTimeSettingHandler;
        notificationTimeSettingHandler.Next = finish;

        var userSettings = new UserSettings { UserId = chatId };
        cache.Set($"{SettingsCachePrefix}{chatId}", userSettings, TimeSpan.FromMinutes(5));

        cache.Set(chainKey, addChannelHandler, TimeSpan.FromMinutes(2));

        await addChannelHandler.ShowStepAsync(update);
    }
}