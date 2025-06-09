namespace SummyAITelegramBot.Core.Bot.Features.Channel.DTO;

public record ChannelPost
{
    public string Text { get; set; }

    public DateTime Date { get; set; }
}