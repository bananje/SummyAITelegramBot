using SummyAITelegramBot.Core.Abstractions;
using SummyAITelegramBot.Core.Bot.Features.Subsciption.Abstractions;
using SummyAITelegramBot.Core.Domain.Models;

namespace SummyAITelegramBot.Core.Bot.Features.Subsciption.Services;

public class SubscriptionService(
    IUnitOfWork unitOfWork) : ISubscriptionService
{
    public async Task SetTrialSubscriptionAsync(long userId)
    {
        var userRepository = unitOfWork.Repository<long, Domain.Models.User>();
        var subscriptionRepository = unitOfWork.Repository<Guid, Subscription>();

        var user = await userRepository.GetByIdAsync(userId) 
            ?? throw new Exception("User not found for applying trial sub");

        var trialSubscription = new Subscription()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(31),
            Type = Domain.Enums.SubscriptionType.TrialSubscription
        };

        var userSubscription = await subscriptionRepository.CreateOrUpdateAsync(trialSubscription);

        user.Subscription = userSubscription;
        user.HasSubscriptionPremium = true;
        
        await userRepository.UpdateAsync(user);

        await unitOfWork.CommitAsync();     
    }
}