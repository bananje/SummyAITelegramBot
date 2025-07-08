using SummyAITelegramBot.Core.Abstractions;

namespace SummyAITelegramBot.Core.Domain.Models;

public class SentUserPost : Entity<int>
{
    public long UserId { get; set; }
    public int ChannelPostId { get; set; }
    public long ChannelId { get; set; }

    public ChannelPost ChannelPost { get; set; }
    public DateTime SentAt { get; set; }
}