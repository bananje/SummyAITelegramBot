namespace SummyAITelegramBot.Core.Bot.Attributes;

public class CommandHandlerAttribute : Attribute
{
    public string CommandName { get; }

    public CommandHandlerAttribute(string commandName)
    {
        CommandName = commandName.ToLowerInvariant();
    }
}