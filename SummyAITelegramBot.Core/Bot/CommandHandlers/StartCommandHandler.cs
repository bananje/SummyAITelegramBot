using Microsoft.Extensions.Logging;
using SummyAITelegramBot.Core.Abstractions;
using SummyAITelegramBot.Core.Bot.Abstractions;
using SummyAITelegramBot.Core.Bot.Attributes;
using SummyAITelegramBot.Core.Bot.Features.User.Abstractions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace SummyAITelegramBot.Core.Bot.CommandHandlers;

/// <summary>
/// Обработчик команды /start
/// </summary>
[CommandHandler("start")]
public class StartCommandHandler(
    ITelegramBotClient botClient, ILogger<StartCommandHandler> logger,
    IUserService userService,
    IStaticImageService imageService,
    IRepository<long, Domain.Models.User> userRepository) : ICommandHandler
{
    public async Task HandleAsync(Message message)
    {
        string text = "";
        var user = await userRepository.GetByIdAsync(message.From.Id);

        if (user?.LastInteractionAt is not null)
        {
             text = $"""
                <b>{message.From.FirstName}, давно не виделись!</b>

                Напомню о себе. Я Summy‑Сова 🦉 — летаю по веткам чатов, собираю ключевые факты и вношу их в аккуратные свитки‑резюме 📜

                <b>Как я работаю?</b>
                1️⃣ <b>Шаг 1:</b> Ты добавляешь интересующие тебя каналы
                2️⃣ <b>Шаг 2:</b> Делаем быструю настройку для твоего удобства
                3️⃣ <b>Шаг 3:</b> Воля, ты получаешь короткие и информативные сводки
                """;
        }
        else
        {
            text = $"""
                <b>{message.From.FirstName}, добро пожаловать!</b>

                Я Summy‑Сова 🦉 — летаю по веткам чатов, собираю ключевые факты и вношу их в аккуратные свитки‑резюме 📜

                <b>Как я работаю?</b>
                1️⃣ <b>Шаг 1:</b> Ты добавляешь интересующие тебя каналы
                2️⃣ <b>Шаг 2:</b> Делаем быструю настройку для твоего удобства
                3️⃣ <b>Шаг 3:</b> Воля, ты получаешь короткие и информативные сводки
                """;
        }

       await userService.UpdateOrCreateUserByTelegramAsync(message.From.Id, message);

        var keyboard = new InlineKeyboardMarkup(new[]
        {
             new[] { InlineKeyboardButton.WithCallbackData("🚀 Полетели", "start") },
             new[] { InlineKeyboardButton.WithCallbackData("✖️ Стоп", "stop") },
        });

        await using var stream = imageService.GetImageStream("summy_start.png");
        await botClient.SendPhoto(
            chatId: message.Chat.Id,
            photo: new InputFileStream(stream),
            caption: text,
            parseMode: ParseMode.Html,
            replyMarkup: keyboard
        );
    }
}