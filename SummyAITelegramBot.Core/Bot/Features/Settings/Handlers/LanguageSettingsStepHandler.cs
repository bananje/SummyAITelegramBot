using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types;
using SummyAITelegramBot.Core.Bot.Abstractions;
using SummyAITelegramBot.Core.Domain.Models;
using SummyAITelegramBot.Core.Domain.Enums;

namespace SummyAITelegramBot.Core.Bot.Features.Settings.Handlers;

public class LanguageSettingsStepHandler : IChainOfStepsHandler<UserSettings>
{
    public IChainOfStepsHandler<UserSettings>? Next { get; set; }

    public async Task ShowStepAsync(ITelegramBotClient bot, long chatId)
    {
        var keyboard = new InlineKeyboardMarkup(new[]
        {
            InlineKeyboardButton.WithCallbackData("Русский", "settings:lang:ru"),
            InlineKeyboardButton.WithCallbackData("English", "settings:lang:en")
        });

        await bot.SendMessage(chatId, "Выберите язык:", replyMarkup: keyboard);
    }

    public async Task HandleAsync(ITelegramBotClient bot, CallbackQuery query, UserSettings settings)
    {
        if (query.Data == "settings:lang:ru")
            settings.Language = Lanquage.RU;
        else if (query.Data == "settings:lang:en")
            settings.Language = Lanquage.EN;

        if (Next != null)
            await Next.ShowStepAsync(bot, query.Message!.Chat.Id);
    }
}