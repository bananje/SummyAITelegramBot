using Telegram.Bot.Types;
using Telegram.Bot;

namespace SummyAITelegramBot.Core.Bot.Abstractions;

/// <summary>
/// Цепочка шагов для отображения в телеграм
/// </summary>
/// <typeparam name="Entity"></typeparam>
public interface IChainOfStepsHandler<Entity> where Entity : class
{
    /// <summary>
    /// Отображает текущий шаг пользователю (например, отправляет кнопки).
    /// </summary>
    Task ShowStepAsync(ITelegramBotClient bot, long chatId);

    /// <summary>
    /// Обрабатывает выбор пользователя и при необходимости передаёт управление следующему шагу.
    /// </summary>
    Task HandleAsync(ITelegramBotClient bot, CallbackQuery query, Entity? entity = null);

    /// <summary>
    /// Следующий обработчик в цепочке (шаг).
    /// </summary>
    IChainOfStepsHandler<Entity>? Next { get; set; }
}