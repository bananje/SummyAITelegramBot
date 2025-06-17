using Telegram.Bot.Types;

namespace SummyAITelegramBot.Core.Bot.Abstractions;

/// <summary>
/// Фабрика создания обработчиков кол-бэков
/// </summary>
public interface ITelegramUpdateFactory
{
    Task DispatchAsync(Update update, string prefix);
}