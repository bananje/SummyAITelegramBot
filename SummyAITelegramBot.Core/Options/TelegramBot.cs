namespace SummyAITelegramBot.Core.Options;

public class TelegramBot
{
    public static string SectionName = nameof(TelegramBot);

    public string Host { get; set; } = string.Empty;

    public string Token { get; set; } = string.Empty;
}