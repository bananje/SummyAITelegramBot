using SummyAITelegramBot.Core.Bot.Abstractions;
using SummyAITelegramBot.Core.Bot.Attributes;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types;
using SummyAITelegramBot.Core.Abstractions;

namespace SummyAITelegramBot.Core.Bot.CallbackHandlers;

[CallbackHandler("stop")]
public class StopCallbackHandler(ITelegramBotClient bot, IStaticImageService imageService) : ICallbackHandler
{
    public async Task HandleAsync(CallbackQuery query)
    {
        var welcomeText = $"""
            <b>Посижу отдохну. Чтобы позвать меня, нажми СТАРТ</b>
            """;

        var keyboard = new InlineKeyboardMarkup(new[]
        {
             new[] { InlineKeyboardButton.WithCallbackData("🚀 Старт", "/start") }
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