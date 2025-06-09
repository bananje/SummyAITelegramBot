using Microsoft.Extensions.DependencyInjection;
using SummyAITelegramBot.Core.AI.Abstractions;
using SummyAITelegramBot.Core.AI.Attributes;
using SummyAITelegramBot.Core.Domain.Enums;
using SummyAITelegramBot.Core.Domain.Models;
using System.Reflection;

namespace SummyAITelegramBot.Core.AI.Factories;

public class SummarizationStrategyFactory : ISummarizationStrategyFactory
{
    private readonly IServiceScopeFactory _scopeFactory;
    private static readonly Dictionary<string, Type> _strategies = new();

    public SummarizationStrategyFactory(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
        LoadStrategies();
    }

    private void LoadStrategies()
    {
        var types = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t =>
                typeof(ISummarizationStrategy).IsAssignableFrom(t) &&
                !t.IsAbstract &&
                t.GetCustomAttribute<SummarizationStrategyAttribute>() is not null);

        foreach (var type in types)
        {
            var attr = type.GetCustomAttribute<SummarizationStrategyAttribute>();
            _strategies[attr!.Key] = type;
        }
    }

    public ISummarizationStrategy Create(UserSettings settings)
    {
        string key = "";

        switch (settings.AiModel)
        {
            case AiModel.DeepSeek:
                key = AiModel.DeepSeek.ToString();
            break;

            case AiModel.OpenAI:
                key = AiModel.OpenAI.ToString();
            break;

            default:
                throw new InvalidOperationException($"Unsupported AI model: {settings.AiModel}.");
        }

        if (!_strategies.TryGetValue(key, out var strategyType))
            throw new InvalidOperationException($"Strategy '{key}' not found");

        using var scope = _scopeFactory.CreateScope();
        return (ISummarizationStrategy)scope.ServiceProvider.GetRequiredService(strategyType);
    }
}
