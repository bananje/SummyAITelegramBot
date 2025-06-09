namespace SummyAITelegramBot.Core.AI.Abstractions;

public interface ISummarizationStrategy
{
    Task<string> SummarizeAsync(string inputText);
}