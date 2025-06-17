using Telegram.Bot.Types;

namespace SummyAITelegramBot.Core.Bot.Abstractions;

public interface IReplyHandler
{
    Task HandleAsync(Message replyMessage);
}