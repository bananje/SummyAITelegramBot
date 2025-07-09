using SummyAITelegramBot.Core.Abstractions;
using SummyAITelegramBot.Core.Bot.Abstractions;
using SummyAITelegramBot.Core.Bot.Attributes;
using SummyAITelegramBot.Core.Bot.Extensions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace SummyAITelegramBot.Core.Bot.Handlers;

[TelegramUpdateHandler("/complete")]
public class CompleteHandler(
    IStaticImageService imageService,
    ITelegramBotClient bot) : ITelegramUpdateHandler
{
    public async Task HandleAsync(Update update)
    {
        var message = update.Message is null 
            ? update.CallbackQuery.Message 
            : update.Message;
        var userId = message.Chat.Id;

        var text = $"""
                👍 Summy к вашим услугам

                Теперь вы будете каждый день получать сводки в указанное время из ваших любимых каналов!
                """;

        var imagePath = "summy_time.jpg";

        var keyboard = new InlineKeyboardMarkup(new[]
        {
             new[] { InlineKeyboardButton.WithCallbackData("🦉 Личный кабинет", "/account") },
        });

        await using var stream = imageService.GetImageStream(imagePath);
        await bot.ReactivelySendPhotoAsync(
            userId,
            photo: new InputFileStream(stream),
            userMessage: message,
            caption: text,
            replyMarkup: keyboard
        );
    }
}