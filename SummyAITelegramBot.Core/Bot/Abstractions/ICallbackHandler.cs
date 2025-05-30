using Telegram.Bot.Types;

namespace SummyAITelegramBot.Core.Bot.Abstractions;

/// <summary>
///  Менеджер обработки колл-бека в зависимости от маршрутизатора
/// </summary>
public interface ICallbackHandler 
{
    /// <summary>
    /// Обработать текущий кол-бэк
    /// </summary>
    /// <param name="query"></param>
    /// <returns></returns>
    Task HandleAsync(CallbackQuery query);

    /// <summary>
    /// Запустить цепочку связанных кол-бэков
    /// Not implemented, если цепочка не требуется
    /// </summary>
    /// <param name="bot"></param>
    /// <param name="chatId"></param>
    /// <returns></returns>
    Task StartChainAsync(long chatId) => Task.CompletedTask;
}