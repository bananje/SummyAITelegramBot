namespace SummyAITelegramBot.Core.AI.Abstractions;

public interface ISummarizationStrategy
{
    Task<string> SummarizeAsync(string inputText);

    Task<bool> ValidateOfUniqueTextAsync(string allTexts, string currentText);

    Task<bool> СheckForAdvertising(string inputText);
}