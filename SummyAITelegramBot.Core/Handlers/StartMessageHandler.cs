using SummyAITelegramBot.Core.Abstractions;
using SummyAITelegramBot.Core.Utils;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace SummyAITelegramBot.Core.Handlers;

[CommandHandler("start")]
public class StartMessageHandler : IMessageHandler
{
    private readonly ITelegramBotClient _botClient;

    public StartMessageHandler(ITelegramBotClient botClient)
    {
        _botClient = botClient;
    }

    public async Task HandleAsync(Message message)
    {
        var keyboard = new ReplyKeyboardMarkup(new[]
        {
            new[] { new KeyboardButton("Настройки"), new KeyboardButton("Мои каналы") },
            new[] { new KeyboardButton("Язык"), new KeyboardButton("Подписка") }
        })
        { ResizeKeyboard = true };

        await _botClient.SendMessage(
            chatId: message.Chat.Id,
            text: "Добро пожаловать! 🚀\nВыберите действие:",
            replyMarkup: keyboard
        );
    }
}