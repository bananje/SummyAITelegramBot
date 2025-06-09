using SummyAITelegramBot.Core.AI.Abstractions;
using System.Text.Json;
using System.Text;
using SummyAITelegramBot.Core.AI.Attributes;

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
            model = "deepseek/deepseek-r1-0528:free",
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
}