using SummyAITelegramBot.Core.Domain.Models;

namespace SummyAITelegramBot.Core.Bot.Features.Channel.Abstractions;

public interface ITelegramSenderService
{
    Task ResolveNotifyUsersAsync(ChannelPost post);

    Task SendSubscriptionOffersToEligibleUsersAsync();
}
