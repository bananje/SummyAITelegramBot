namespace SummyAITelegramBot.Core.Bot.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class TelegramUpdateHandlerAttribute : Attribute
{
    public string Prefix { get; }

    public bool HasChain { get; }

    public TelegramUpdateHandlerAttribute(string prefix, bool hasChain)
    {
        Prefix = prefix;
        HasChain = hasChain;
    }
}