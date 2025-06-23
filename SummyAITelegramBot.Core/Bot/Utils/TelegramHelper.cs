using System.Text.RegularExpressions;

namespace SummyAITelegramBot.Core.Bot.Utils;

public class TelegramHelper
{
    public static bool IsTelegramChannelLink(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return false;

        input = input.Trim();

        // Обычные username-ссылки
        var usernamePattern = @"^@[a-zA-Z0-9_]{5,32}$";

        // Ссылки вида t.me/username или telegram.me/username
        var publicLinkPattern = @"^(https?:\/\/)?(t\.me|telegram\.me)\/[a-zA-Z0-9_]{5,32}$";

        // Приватные ссылки с плюсом и токеном
        var privateInvitePattern = @"^(https?:\/\/)?t\.me\/\+[a-zA-Z0-9_-]+$";

        return Regex.IsMatch(input, usernamePattern, RegexOptions.IgnoreCase) ||
               Regex.IsMatch(input, publicLinkPattern, RegexOptions.IgnoreCase) ||
               Regex.IsMatch(input, privateInvitePattern, RegexOptions.IgnoreCase);
    }
}