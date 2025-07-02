using SummyAITelegramBot.Core.Abstractions;
using SummyAITelegramBot.Core.Domain.Enums;

namespace SummyAITelegramBot.Core.Domain.Models;

public class Subscription : Entity<Guid>
{
    public bool HasAutoPayment { get; set; }

    public DateTimeOffset EndDate { get; set; }

    public DateTimeOffset StartDate { get; set; }

    public SubscriptionType Type { get; set; }

    public long UserId { get; set; }

    public User User { get; set; }
}