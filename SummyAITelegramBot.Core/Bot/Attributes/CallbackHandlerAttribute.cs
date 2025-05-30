namespace SummyAITelegramBot.Core.Bot.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class CallbackHandlerAttribute : Attribute
{
    public string Prefix { get; }

    public CallbackHandlerAttribute(string prefix)
    {
        Prefix = prefix;
    }
}