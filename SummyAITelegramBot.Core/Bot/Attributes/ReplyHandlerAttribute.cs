namespace SummyAITelegramBot.Core.Bot.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class ReplyHandlerAttribute : Attribute
{
    public string CommandName { get; }

    public ReplyHandlerAttribute(string commandName)
    {
        CommandName = commandName.ToLowerInvariant();
    }
}
