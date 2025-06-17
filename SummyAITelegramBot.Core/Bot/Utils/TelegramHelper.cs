using System.Text.RegularExpressions;

namespace SummyAITelegramBot.Core.Bot.Utils;

public class TelegramHelper
{
    public static bool IsTelegramChannelLink(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return false;

        input = input.Trim();

        var urlPattern = @"^(https?:\/\/)?t\.me\/[a-zA-Z0-9_]{5,32}$";

        var usernamePattern = @"^@[a-zA-Z0-9_]{5,32}$";

        return Regex.IsMatch(input, urlPattern) || Regex.IsMatch(input, usernamePattern);
    }
}