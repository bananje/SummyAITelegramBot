using SummyAITelegramBot.Core.Abstractions;
using SummyAITelegramBot.Core.Bot.Abstractions;
using SummyAITelegramBot.Core.Bot.Attributes;
using SummyAITelegramBot.Core.Bot.Extensions;
using SummyAITelegramBot.Core.Bot.Utils;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace SummyAITelegramBot.Core.Bot.Handlers;

[TelegramUpdateHandler("/account")]
public class AccountHadler(
    ITelegramBotClient botClient,
    IStaticImageService imageService) : ITelegramUpdateHandler
{
    public async Task HandleAsync(Update update)
    {
        var chatInfo = TelegramHelper.GetUserAndChatId(update);

        var text = $"""
                <b>👋Summy к вашим услугам!</b>

                Текст личного кабинета
                """;
        var imagePath = "summy_account.jpg";

        var keyboard = new InlineKeyboardMarkup(new[]
        {            
            new[] { InlineKeyboardButton.WithCallbackData("📣 Добавить канал", "/add") },
            new[] { InlineKeyboardButton.WithCallbackData("❌ Удалить канал", "/mychannels") },
            new[] { InlineKeyboardButton.WithCallbackData("⚙️ Настроить время", "/showchannelsettings") },
            new[] { InlineKeyboardButton.WithCallbackData("💵 Подписка", "/showsubscription") },
        });

        await using var stream = imageService.GetImageStream(imagePath);
        await botClient.ReactivelySendPhotoAsync(
            chatInfo.chatId,
            photo: new InputFileStream(stream),
            userMessage: update.Message,
            caption: text,
            replyMarkup: keyboard
        );
    }
}