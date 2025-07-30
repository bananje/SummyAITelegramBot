using SummyAITelegramBot.Core.Abstractions;

namespace SummyAITelegramBot.Core.Domain.Models;

public class DelayedUserPost : Entity<long>
{
    public long UserId { get; set; }
    public User User { get; set; }

    public bool IsSend { get; set; }

    public long ChannelId { get; set; }

    public DateTimeOffset? CreatedDate { get; set; }

    public int ChannelPostId { get; set; }
    public ChannelPost ChannelPost { get; set; }
}