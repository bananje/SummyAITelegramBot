using Microsoft.Extensions.Logging;
using SummyAITelegramBot.Core.Bot.Abstractions;
using SummyAITelegramBot.Core.Bot.Attributes;
using SummyAITelegramBot.Core.Bot.Features.User.Abstractions;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace SummyAITelegramBot.Core.Bot.CommandHandlers;

/// <summary>
/// Обработчик команды /start
/// </summary>
[CommandHandler("start")]
public class StartCommandHandler(
    ITelegramBotClient botClient, ILogger<StartCommandHandler> logger,
    IUserService userService) : ICommandHandler
{
    public async Task HandleAsync(Message message)
    {
        // получить предоставляемую тг информацию о пользователе
        await userService.GetUserInfoFromTelegramAsync(message);

        await botClient.SendMessage(
            chatId: message.Chat.Id,
            text: "Добро пожаловать! 🚀\nВыберите действие:"
        );
    }

    private 
}