using SummyAITelegramBot.Core.Bot.Abstractions;
using SummyAITelegramBot.Core.Domain.Models;

namespace SummyAITelegramBot.Core.Bot.Extensions;

public static class StepHandlerExtensions
{
    public static THandler SetNext<THandler>(this THandler current, IChainOfStepsHandler<UserSettings> next)
        where THandler : IChainOfStepsHandler<UserSettings>
    {
        current.Next = next;
        return current;
    }
}