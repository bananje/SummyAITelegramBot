namespace SummyAITelegramBot.Core.AI.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class SummarizationStrategyAttribute : Attribute
{
    public string Key { get; }

    public SummarizationStrategyAttribute(string key)
    {
        Key = key;
    }
}