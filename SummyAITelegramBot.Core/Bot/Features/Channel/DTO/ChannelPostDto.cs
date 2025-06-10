namespace SummyAITelegramBot.Core.Bot.Features.Channel.DTO;

public record ChannelPostDto
{
    public int Id { get; set; }

    public string Text { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public long ChannelId { get; set; }
}