using SummyAITelegramBot.Core.Bot.Abstractions;
using SummyAITelegramBot.Core.Domain.Models;

namespace SummyAITelegramBot.Core.Bot.Extensions;

public static class StepHandlerExtensions
{
    public static THandler SetNext<THandler>(this THandler current, IStepOnChainHandler<UserSettings> next)
        where THandler : IStepOnChainHandler<UserSettings>
    {
        current.Next = next;
        return current;
    }
}