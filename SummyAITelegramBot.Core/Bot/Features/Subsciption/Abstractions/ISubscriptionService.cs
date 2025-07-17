namespace SummyAITelegramBot.Core.Bot.Features.Subsciption.Abstractions;

public interface ISubscriptionService
{
    Task SetTrialSubscriptionAsync(long userId);
}
