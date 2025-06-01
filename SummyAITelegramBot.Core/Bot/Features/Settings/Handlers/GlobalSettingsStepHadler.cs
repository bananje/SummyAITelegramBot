using SummyAITelegramBot.Core.Bot.Abstractions;
using SummyAITelegramBot.Core.Domain.Models;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace SummyAITelegramBot.Core.Bot.Features.Settings.Handlers;

public class GlobalSettingsStepHadler : IChainOfStepsHandler<UserSettings>
{
    public IChainOfStepsHandler<UserSettings>? Next { get; set; }

    public async Task HandleAsync(ITelegramBotClient bot, CallbackQuery query, UserSettings entity)
    {
        if (query.Data == "settings:global:yes")
            entity.IsGlobal = true;
        else if (query.Data == "settings:global:no")
            entity.IsGlobal = false;

        if (Next != null)
            await Next.ShowStepAsync(bot, query.Message!.Chat.Id);
    }

    public async Task ShowStepAsync(ITelegramBotClient bot, long chatId)
    {
        var keyboard = new InlineKeyboardMarkup(new[]
         {
            InlineKeyboardButton.WithCallbackData("Да", "settings:global:yes"),
            InlineKeyboardButton.WithCallbackData("Нет", "settings:global:no")
        });

        await bot.SendMessage(chatId, "Применить настройку для всех каналов?", replyMarkup: keyboard);
    }
}