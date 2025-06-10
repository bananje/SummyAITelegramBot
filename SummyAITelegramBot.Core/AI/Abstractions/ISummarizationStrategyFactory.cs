using SummyAITelegramBot.Core.Domain.Enums;

namespace SummyAITelegramBot.Core.AI.Abstractions;

public interface ISummarizationStrategyFactory
{
    ISummarizationStrategy Create(AiModel aiModel);
}