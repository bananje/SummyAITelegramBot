using FuzzySharp;
using SummyAITelegramBot.Core.AI.Abstractions;
using SummyAITelegramBot.Core.AI.Attributes;
using System.Text.RegularExpressions;

namespace SummyAITelegramBot.Core.AI.AiStrategies;

[SummarizationStrategy("TextHeader")]
public class TextHeaderSummarizationStrategy : ISummarizationStrategy
{
    public Task<string> SummarizeAsync(string inputText)
    {
        if (string.IsNullOrWhiteSpace(inputText))
            return Task.FromResult(string.Empty);

        // Ищет первое предложение, заканчивающееся на . ! или ?
        var match = Regex.Match(
            inputText.Trim(), @"^.*?[\.!?](\s|$)", 
            RegexOptions.Singleline);
        
        var result = match.Success ? match.Value.Trim() : inputText.Trim();

        return Task.FromResult(result);
    }

    public Task<bool> ValidateOfUniqueTextAsync(string allTexts, string currentText)
    {
        if (string.IsNullOrWhiteSpace(allTexts) || string.IsNullOrWhiteSpace(currentText))
            return Task.FromResult(true); // Нет с чем сравнивать — считаем уникальным

        var allTextList = allTexts.Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var text in allTextList)
        {
            int similarity = Fuzz.Ratio(currentText, text);

            if (similarity >= 90)
                return Task.FromResult(false);
        }

        return Task.FromResult(true); 
    }
}
