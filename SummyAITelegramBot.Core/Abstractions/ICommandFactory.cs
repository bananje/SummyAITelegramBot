using Telegram.Bot.Types;

namespace SummyAITelegramBot.Core.Abstractions;

/// <summary>
/// Фабрика создания ТГ команд
/// </summary>
public interface ICommandFactory
{
    Task ProcessCommandAsync(string command, Message message);
}