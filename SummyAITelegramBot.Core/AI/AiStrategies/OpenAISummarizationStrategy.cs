using SummyAITelegramBot.Core.AI.Abstractions;
using System.Text.Json;
using System.Text;
using SummyAITelegramBot.Core.AI.Attributes;

namespace SummyAITelegramBot.Core.AI.AiStrategies;

[SummarizationStrategy("OpenAi")]
public class OpenAISummarizationStrategy : ISummarizationStrategy
{
    private readonly HttpClient _client;

    public OpenAISummarizationStrategy(IHttpClientFactory factory)
    {
        _client = factory.CreateClient("OpenAI");
    }

    public async Task<string> SummarizeAsync(string inputText)
    {
        var request = new
        {
            model = "gpt-4", // или gpt-3.5-turbo
            messages = new[]
            {
                new { role = "system", content = "Ты помогаешь делать краткие саммари новостей." },
                new { role = "user", content = $"Сделай краткую выжимку:\n\n{inputText}" }
            },
            temperature = 0.7
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
            .GetString() ?? "[OpenAI: пустой ответ]";
    }

    public Task<bool> ValidateOfUniqueTextAsync(string allTexts, string currentText)
    {
        throw new NotImplementedException();
    }
}