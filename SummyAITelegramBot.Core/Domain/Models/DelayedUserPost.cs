using SummyAITelegramBot.Core.Abstractions;

namespace SummyAITelegramBot.Core.Domain.Models;

public class DelayedUserPost : Entity<long>
{
    public long UserId { get; set; }
    public User User { get; set; }

    public int ChannelPostId { get; set; }
    public ChannelPost ChannelPost { get; set; }

    public DateTime AddedDate { get; set; } = DateTime.UtcNow;
}