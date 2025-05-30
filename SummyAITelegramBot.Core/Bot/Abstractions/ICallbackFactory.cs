using Telegram.Bot.Types;

namespace SummyAITelegramBot.Core.Bot.Abstractions;

/// <summary>
/// Фабрика создания обработчиков кол-бэков
/// </summary>
public interface ICallbackFactory
{
    Task DispatchAsync(CallbackQuery callbackQuery);
}