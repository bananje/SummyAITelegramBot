using SummyAITelegramBot.Core.Abstractions;
using SummyAITelegramBot.Core.AI.Abstractions;
using SummyAITelegramBot.Core.Bot.Abstractions;
using SummyAITelegramBot.Core.Bot.Attributes;
using SummyAITelegramBot.Core.Bot.Features.Channel.Services;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace SummyAITelegramBot.Core.Bot.Handlers;

[TelegramUpdateHandler("/groupedposts")]
public class GroupedPostsHandler(
    ITelegramBotClient bot, 
    IUnitOfWork unitOfWork, 
    ISummarizationStrategyFactory summarizationStrategyFactory) : ITelegramUpdateHandler
{
    public async Task HandleAsync(Update update)
    {
        var data = update.CallbackQuery?.Data;
        var pageStr = data?.Split(':').LastOrDefault();
        if (int.TryParse(pageStr, out var page))
        {
            var sender = new TelegramSenderService(bot, summarizationStrategyFactory, unitOfWork);
            await sender.SendGroupedPostsAsync(update.CallbackQuery.From.Id, page);
        }
    }
}