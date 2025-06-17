using Microsoft.Extensions.Logging;
using SummyAITelegramBot.Core.Bot.Abstractions;
using SummyAITelegramBot.Core.Bot.Attributes;
using SummyAITelegramBot.Core.Bot.Features.Settings;
using Telegram.Bot.Types;

namespace SummyAITelegramBot.Core.Bot.CommandHandlers;

/// <summary>
/// Обработчик команды /settings
/// </summary>
[CommandHandler("settings")]
public class SettingsCommandHandler(
    ILogger<SettingsCommandHandler> logger,
    SettingsChainOfStepsHandler handler
    ) : ICommandHandler
{
    public async Task HandleAsync(Message message)
    {
        //await handler.StartChainAsync(message.Chat.Id, message.From.Id);
    }
}