using SummyAITelegramBot.Core.Domain.Models;

namespace SummyAITelegramBot.Core.AI.Abstractions;

public interface ISummarizationStrategyFactory
{
    ISummarizationStrategy Create(UserSettings settings);
}