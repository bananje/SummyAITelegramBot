namespace SummyAITelegramBot.Core.Bot.Abstractions;

public interface IUserCommandCache
{
    void SetLastCommand(long userId, string command);

    string? GetLastCommand(long userId);
}