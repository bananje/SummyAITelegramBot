using System.Text.RegularExpressions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

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

    public static (long userId, long chatId) GetUserAndChatId(Update update)
    {
        if (update.Message != null)
        {
            return (update.Message.From.Id, update.Message.Chat.Id);
        }

        if (update.CallbackQuery != null)
        {
            return (update.CallbackQuery.From.Id, update.CallbackQuery.Message.Chat.Id);
        }

        throw new Exception("Неизвестный формат обновления");
    }

    public static Telegram.Bot.Types.User? GetUserFromUpdate(Update update)
    {
        return update.Type switch
        {
            UpdateType.Message => update.Message?.From,
            UpdateType.EditedMessage => update.EditedMessage?.From,
            UpdateType.CallbackQuery => update.CallbackQuery?.From,
            UpdateType.InlineQuery => update.InlineQuery?.From,
            UpdateType.ChosenInlineResult => update.ChosenInlineResult?.From,
            UpdateType.ChannelPost => update.ChannelPost?.From,
            UpdateType.EditedChannelPost => update.EditedChannelPost?.From,
            UpdateType.MyChatMember => update.MyChatMember?.From,
            UpdateType.ChatMember => update.ChatMember?.From,
            UpdateType.ChatJoinRequest => update.ChatJoinRequest?.From,
            _ => null
        };
    }
}