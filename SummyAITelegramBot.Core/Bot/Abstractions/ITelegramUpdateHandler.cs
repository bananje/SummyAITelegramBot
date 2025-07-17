using Telegram.Bot.Types;

namespace SummyAITelegramBot.Core.Bot.Abstractions;

/// <summary>
///  Менеджер обработки updates в зависимости от маршрутизатора
/// </summary>
public interface ITelegramUpdateHandler
{
    /// <summary>
    /// Обработать текущий update
    /// </summary>
    /// <param name="query"></param>
    /// <returns></returns>
    Task HandleAsync(Update? update);
}