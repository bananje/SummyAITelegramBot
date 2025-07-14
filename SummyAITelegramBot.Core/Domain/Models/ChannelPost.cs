using SummyAITelegramBot.Core.Abstractions;

namespace SummyAITelegramBot.Core.Domain.Models;

public class ChannelPost : Entity<int>
{
    public string Text { get; set; }

    public DateTime CreatedDate { get; set; }

    public long ChannelId { get; set; }

    public string? MediaPath { get; set; }  

    public Channel Channel { get; set; }

    public DelayedUserPost DelayedUserPost { get; set; }
}