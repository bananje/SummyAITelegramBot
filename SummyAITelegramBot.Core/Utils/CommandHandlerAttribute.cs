namespace SummyAITelegramBot.Core.Utils;

public class CommandHandlerAttribute : Attribute
{
    public string CommandName { get; }

    public CommandHandlerAttribute(string commandName)
    {
        CommandName = commandName.ToLowerInvariant();
    }
}