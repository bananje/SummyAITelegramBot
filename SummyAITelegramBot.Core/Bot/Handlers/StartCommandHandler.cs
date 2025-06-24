using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SummyAITelegramBot.Core.Abstractions;
using SummyAITelegramBot.Core.Bot.Abstractions;
using SummyAITelegramBot.Core.Bot.Attributes;
using SummyAITelegramBot.Core.Bot.Extensions;
using SummyAITelegramBot.Core.Bot.Features.User.Abstractions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace SummyAITelegramBot.Core.Bot.Handlers;

/// <summary>
/// Обработчик команды /start
/// </summary>
[TelegramUpdateHandler("/start")]
public class StartCommandHandler(
    ITelegramBotClient botClient, ILogger<StartCommandHandler> logger,
    IUserService userService,
    IMemoryCache cache,
    IStaticImageService imageService,
    IRepository<long, Domain.Models.User> userRepository) : ITelegramUpdateHandler
{
    public async Task HandleAsync(Update upd)
    {
        var message = upd.Message;
        string text = "";
        var user = await userRepository.GetByIdAsync(message.From.Id);
        string imagePath = "";

        if (user?.LastInteractionAt is not null)
        {
            text = $"""
                <b>{message.From.FirstName}, рада снова Вас видеть!</b>

                Я Summy‑Сова 🦉 — летаю по веткам чатов, собираю ключевые факты и вношу их в аккуратные свитки‑резюме 📜

                <b>Как я работаю?</b>
                1️⃣ Добавим ваш канал
                2️⃣ Укажем время получения сводоксводки
                """;

            imagePath = "summy_time.jpg";
        }
        else
        {
            text = $"""
                <b>{message.From.FirstName}, добро пожаловать!</b>

                Я Summy‑Сова 🦉 — летаю по веткам чатов, собираю ключевые факты и вношу их в аккуратные свитки‑резюме 📜

                <b>Настроим вашу сводку</b>
                1️⃣ Добавим ваш канал
                2️⃣ Укажем время получения сводок
                """;

            imagePath = "summy_start.png";
        }

        await userService.UpdateOrCreateUserByTelegramAsync(message.From.Id, message);

        var keyboard = new InlineKeyboardMarkup(new[]
        {
             new[] { InlineKeyboardButton.WithCallbackData("🚀 Каналы", "channels") },
             new[] { InlineKeyboardButton.WithCallbackData("✖️ Стоп", "stop") },
        });

        await using var stream = imageService.GetImageStream(imagePath);
        await botClient.ReactivelySendPhotoAsync(
            upd.Message.Chat.Id,
            photo: new InputFileStream(stream),
            userMessage: upd.Message,
            caption: text,
            replyMarkup: keyboard
        );
    }
}