using SummyAITelegramBot.Core.Abstractions;
using SummyAITelegramBot.Core.Bot.Abstractions;
using SummyAITelegramBot.Core.Bot.Attributes;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace SummyAITelegramBot.Core.Bot.CallbackHandlers;

/// <summary>
/// Окно с обучением пользователя при старте бота
/// </summary>
/// <param name="bot"></param>
/// <param name="imageService"></param>

public class StartCallbackHandler(
    ITelegramBotClient bot,
    IStaticImageService imageService
    ) //: ITelegramUpdateHandler
{
    public async Task HandleAsync(CallbackQuery query)
    {
        var welcomeText = $"""
            <b>{query.From.FirstName}, немного расскажу, как мной управлять!</b>

            Снизу слева у тебя есть меню с командами, чтобы давать их мне.

            Через команду /settings ты можешь задать настройки глобально для каналов, но ты также 
            можешь делать отдельную настройку под конкретный канал.
            
            В личном кабинете ты можешь управлять своим тарифом и смотреть статистику реферальной программы
            Будет дополняться...
            """;

        var keyboard = new InlineKeyboardMarkup(new[]
        {
             new[] { InlineKeyboardButton.WithCallbackData("⚙️ Настройки", "/settings") },
             new[] { InlineKeyboardButton.WithCallbackData("➕ Добавить канал", "/add") },

        });

        await using var stream = imageService.GetImageStream("summy_start.png");
        await bot.SendPhoto(
            chatId: query.Message.Chat.Id,
            photo: new InputFileStream(stream),
            caption: welcomeText,
            parseMode: ParseMode.Html,
            replyMarkup: keyboard
        );
    }
}