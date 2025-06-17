using Telegram.Bot.Types;

namespace SummyAITelegramBot.Core.Bot.Abstractions;

/// <summary>
/// Фабрика создания обработчиков ответов юзера
/// </summary>
public interface IReplyFactory
{
    Task DispatchAsync(Message replyMessage);
}