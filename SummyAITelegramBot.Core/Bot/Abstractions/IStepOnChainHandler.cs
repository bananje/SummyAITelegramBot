using Telegram.Bot.Types;

namespace SummyAITelegramBot.Core.Bot.Abstractions;

/// <summary>
/// Цепочка шагов для отображения в телеграм
/// </summary>
/// <typeparam name="Entity"></typeparam>
public interface IStepOnChainHandler<Entity> where Entity : class
{
    /// <summary>
    /// Отображает текущий шаг пользователю (например, отправляет кнопки).
    /// </summary>
    Task ShowStepAsync(Update update);

    /// <summary>
    /// Обрабатывает выбор пользователя и при необходимости передаёт управление следующему шагу.
    /// </summary>
    Task HandleAsync(Update update, Entity? entity = null);

    /// <summary>
    /// Следующий обработчик в цепочке (шаг).
    /// </summary>
    IStepOnChainHandler<Entity>? Next { get; set; }
}

/// <summary>
/// Цепочка шагов для отображения в телеграм
/// </summary>
/// <typeparam name="Entity"></typeparam>
public interface IStepOnChainHandler
{
    /// <summary>
    /// Отображает текущий шаг пользователю (например, отправляет кнопки).
    /// </summary>
    Task ShowStepAsync(Update update);

    /// <summary>
    /// Обрабатывает выбор пользователя и при необходимости передаёт управление следующему шагу.
    /// </summary>
    Task HandleAsync(Update update);

    /// <summary>
    /// Следующий обработчик в цепочке (шаг).
    /// </summary>
    IStepOnChainHandler? Next { get; set; }
}