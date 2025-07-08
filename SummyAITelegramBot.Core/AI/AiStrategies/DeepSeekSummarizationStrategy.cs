using SummyAITelegramBot.Core.AI.Abstractions;
using System.Text.Json;
using System.Text;
using SummyAITelegramBot.Core.AI.Attributes;
using System.Text.RegularExpressions;

namespace SummyAITelegramBot.Core.AI.AiStrategies;

[SummarizationStrategy("DeepSeek")]
public class DeepSeekSummarizationStrategy : ISummarizationStrategy
{
    private readonly HttpClient _client;
    public DeepSeekSummarizationStrategy(IHttpClientFactory factory)
    {
        _client = factory.CreateClient("DeepSeek");
    }

    public async Task<string> SummarizeAsync(string inputText)
    {
        var request = new
        {
            model = "deepseek/deepseek-r1-0528-qwen3-8b:free",
            messages = new[]
                {
                    new { role = "system", content = "Ты — ассистент, делающий краткие, ёмкие сводки длинных текстов." },
                    new { role = "user", content = $"Сделай короткую сводку следующего текста:\n\n{inputText}" }
                }
        };

        var content = new StringContent(
                JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("chat/completions", content);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        return doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? "[OpenRouter: пустой ответ]";
    }

    public async Task<bool> ValidateOfUniqueTextAsync(string allTexts, string currentText)
    {
        var prompt = $"""
            У тебя есть список предыдущих постов и новый пост. 
            Ответь только "true" (если новый пост по смыслу уникален) 
            или "false" (если он дублирует или почти повторяет предыдущие посты).

            Предыдущие посты:
            ---
            {allTexts}
            ---

            Новый пост:
            ---
            {currentText}
            ---

            Ответь строго: true или false. Без пояснений.
        """;

        var request = new
        {
            model = "deepseek/deepseek-r1-0528-qwen3-8b:free",
            messages = new[]
            {
            new { role = "system", content = "Ты — ассистент, проверяющий уникальность постов. Отвечай строго: true или false. Без объяснений." },
            new { role = "user", content = prompt }
        }
        };

        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("chat/completions", content);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        var answerRaw = doc.RootElement
        .GetProperty("choices")[0]
        .GetProperty("message")
        .GetProperty("content")
        .GetString();

        var match = Regex.Match(answerRaw ?? "", @"\b(true|false)\b", RegexOptions.IgnoreCase);
        if (!match.Success)
            throw new Exception($"Невалидный ответ от модели: {answerRaw}");

        var answer = match.Value.ToLowerInvariant();

        return answer == "true";

        // Лог или выброс исключения, если ответ невалидный
        throw new Exception($"Невалидный ответ от модели: {answerRaw}");
    }
}