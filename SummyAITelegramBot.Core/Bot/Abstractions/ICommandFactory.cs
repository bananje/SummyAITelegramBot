using Telegram.Bot.Types;

namespace SummyAITelegramBot.Core.Bot.Abstractions;

/// <summary>
/// Фабрика создания обработчиков ТГ команд
/// </summary>
public interface ICommandFactory
{
    Task ProcessCommandAsync(string command, Message message);
}