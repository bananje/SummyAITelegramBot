using SummyAITelegramBot.Core.Bot.Abstractions;
using SummyAITelegramBot.Core.Domain.Models;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace SummyAITelegramBot.Core.Bot.Features.Settings.Handlers;

public class ChannelReductionStepHandler : IChainOfStepsHandler<UserSettings>
{
    public IChainOfStepsHandler<UserSettings>? Next { get; set; }

    public async Task HandleAsync(ITelegramBotClient bot, CallbackQuery query, UserSettings entity)
    {
        if (query.Data == "settings:channelreduction:yes")
            entity.IsBlockingSimilarPostsInChannels = true;
        else if (query.Data == "settings:channelreductio:no")
            entity.IsBlockingSimilarPostsInChannels = false;

        if (Next != null)
            await Next.ShowStepAsync(bot, query.Message!.Chat.Id);
    }

    public async Task ShowStepAsync(ITelegramBotClient bot, long chatId)
    {
        var keyboard = new InlineKeyboardMarkup(new[]
         {
            InlineKeyboardButton.WithCallbackData("Да", "settings:channelreduction:yes"),
            InlineKeyboardButton.WithCallbackData("Нет", "settings:channelreductio:no")
        });

        await bot.SendMessage(chatId, "Мне присылать похожие по содержанию посты?", replyMarkup: keyboard);
    }
}