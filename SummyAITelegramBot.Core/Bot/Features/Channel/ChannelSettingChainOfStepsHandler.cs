using Microsoft.Extensions.Caching.Memory;
using SummyAITelegramBot.Core.Abstractions;
using SummyAITelegramBot.Core.Bot.Abstractions;
using SummyAITelegramBot.Core.Bot.Attributes;
using SummyAITelegramBot.Core.Bot.Features.Channel.Abstractions;
using SummyAITelegramBot.Core.Bot.Features.Channel.Handlers;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace SummyAITelegramBot.Core.Bot.Features.Channel;

[TelegramUpdateHandler("add", true)]
public class ChannelSettingChainOfStepsHandler(
    ITelegramBotClient bot,
    ITelegramChannelAdapter telegramChannelAdapter,
    IMemoryCache cache,
    IUnitOfWork unitOfWork) : ITelegramUpdateHandler
{
    private const string ChainCachePrefix = "add_channel_chain_";
    private const string ChannelCachePrefix = "add_channel_chain_channel_id";

    public async Task HandleAsync(Update update)
    {
        var chatId = update.Message.Chat.Id;
        var chainKey = $"{ChainCachePrefix}{chatId}";
        var channelIdCachePrefix = $"{ChannelCachePrefix}{chatId}";

        if (!cache.TryGetValue<IStepOnChainHandler>(
                chainKey, out var existingHandler))
        {
            await StartChainAsync(update);
            return;
        }

        await existingHandler.HandleAsync(update);

        if (existingHandler.Next != null)
        {
            cache.Set(chainKey, existingHandler.Next, TimeSpan.FromMinutes(2));
        }
        else
        {
            cache.Remove(chainKey);
            cache.Remove(channelIdCachePrefix);
        }
    }

    public async Task StartChainAsync(Update update)
    {
        var chatId = update.Message.Chat.Id;
        var userId = update.Message.From.Id;
        var chainKey = $"{ChainCachePrefix}{chatId}";
        var channelIdCachePrefix = $"{ChannelCachePrefix}{chatId}";

        var addChannelHandler = new AddChannelHandler(
            unitOfWork, 
            bot, 
            telegramChannelAdapter,
            cache,
            channelIdCachePrefix);

        cache.Set(chainKey, addChannelHandler, TimeSpan.FromMinutes(2));

        await addChannelHandler.ShowStepAsync(update);
    }
}