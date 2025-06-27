using SummyAITelegramBot.Core.Bot.Abstractions;
using SummyAITelegramBot.Core.Domain.Models;

namespace SummyAITelegramBot.Core.Bot.Extensions;

public static class StepHandlerExtensions
{
    public static THandler SetNext<THandler>(this THandler current, IStepOnChainHandler<ChannelUserSettings> next)
        where THandler : IStepOnChainHandler<ChannelUserSettings>
    {
        current.Next = next;
        return current;
    }
}