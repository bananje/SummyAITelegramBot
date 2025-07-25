﻿using Microsoft.Extensions.Logging;
using SummyAITelegramBot.Core.Abstractions;
using SummyAITelegramBot.Core.Bot.Abstractions;
using SummyAITelegramBot.Core.Bot.Attributes;
using SummyAITelegramBot.Core.Bot.Extensions;
using SummyAITelegramBot.Core.Bot.Features.User.Abstractions;
using SummyAITelegramBot.Core.Bot.Utils;
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
    IStaticImageService imageService,
    ITelegramUpdateFactory telegramUpdateFactory,
    IRepository<long, Domain.Models.User> userRepository) : ITelegramUpdateHandler
{
    public async Task HandleAsync(Update upd)
    {
        var message = upd.Message is null ? upd.CallbackQuery.Message
            : upd.Message;

        var (userId, chatId) = TelegramHelper.GetUserAndChatId(upd);

        string text = "";
        var user = await userRepository.GetByIdAsync(chatId);
        string imagePath = "";

        if (user?.LastInteractionAt is not null)
        {
            await telegramUpdateFactory.DispatchAsync(upd, "/account");
            return;
        }
        else
        {
            text = $"""
                <b>👋{message.From.FirstName}, добро пожаловать!</b>

                Я Сова Summy летаю по веткам чатов, собираю ключевые факты и вношу их в аккуратные свитки‑резюме📜

                <b>Настроим Вашу первую сводку:</b>
                1️⃣Добавим Ваши каналы
                2️⃣Укажем время получения постов

                (КНОПКА "🦉 Полетели дальше")
                """;

            imagePath = "summy_start.jpg";
        }

        await userService.UpdateOrCreateUserByTelegramAsync(upd);

        var keyboard = new InlineKeyboardMarkup(new[]
        {
             new[] { InlineKeyboardButton.WithCallbackData("🦉 Полетели дальше", "/add") },
        });

        var stream = imageService.GetImageStream(imagePath);
        await botClient.ReactivelySendPhotoAsync(
            chatId,
            photo: stream,
            userMessage: message,
            caption: text,
            replyMarkup: keyboard
        );
    }
}