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

        inputText = inputText.Trim();

        // Пытаемся найти первое предложение, заканчивающееся на . ! или ?
        var match = Regex.Match(inputText, @"^.*?[\.!?](\s|$)", RegexOptions.Singleline);

        if (match.Success)
        {
            var words = match.Value.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            var summary = string.Join(" ", words.Take(15));
            return Task.FromResult(summary);
        }
        else
        {
            var words = inputText.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            var summary = string.Join(" ", words.Take(15));
            return Task.FromResult(summary);
        }
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
