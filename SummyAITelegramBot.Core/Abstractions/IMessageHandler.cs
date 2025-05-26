using Telegram.Bot.Types;

namespace SummyAITelegramBot.Core.Abstractions;

public interface IMessageHandler
{
    Task HandleAsync(Message message);
}