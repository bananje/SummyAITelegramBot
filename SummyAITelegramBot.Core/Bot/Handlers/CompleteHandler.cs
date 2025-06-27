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
                🦉 Задача выполнена! Сводки будут прилетать в этот чат

                Основные возможности:

                📅 Ежедневные саммари
                Формирую краткую сводку обсуждений в групповом чате и присылаю её прямо в чат.

                👤 Индивидуальные саммари
                Создаю краткие сводки из постов выставляемых в каналах.

                🎙 Сводка голосовых сообщений
                Анализирую голосовые сообщения и выдаю по ним краткую текстовую сводку.

                🤖 Ответы с помощью ИИ
                Отвечаю на вопросы участников, используя нейросеть.

                ✅ Даю доступ к популярным нейросетям
                Предоставлю возможность через меня задать вопрос самым разным нейросетям
                """;

        var imagePath = "summy_time.jpg";

        var keyboard = new InlineKeyboardMarkup(new[]
        {
             new[] { InlineKeyboardButton.WithCallbackData("📣 Каналы", "/add") },
             new[] { InlineKeyboardButton.WithCallbackData("💬 Чаты", "/chat") },
             new[] { InlineKeyboardButton.WithCallbackData("🤖 Написать AI", "/ai") },
             new[] { InlineKeyboardButton.WithCallbackData("❌ Остановить", "/stop") }
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