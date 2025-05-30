using Telegram.Bot.Types;

namespace SummyAITelegramBot.Core.Bot.Abstractions;

public interface ICommandHandler
{
    Task HandleAsync(Message message);
}