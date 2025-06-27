using SummyAITelegramBot.Core.Bot.Abstractions;
using SummyAITelegramBot.Core.Bot.Attributes;
using SummyAITelegramBot.Core.Bot.Extensions;
using SummyAITelegramBot.Core.Bot.Utils;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace SummyAITelegramBot.Core.Bot.Features.Settings.Handlers;

[TelegramUpdateHandler("/shownotificationtimesettings")]
public class ShowNotificationTimeSettingsHandler(
    ITelegramBotClient bot) : ITelegramUpdateHandler
{
    public async Task HandleAsync(Update update)
    {
        var chatId = update.CallbackQuery.Message.Chat.Id;
        var text =
             "⏱️ В какое время ты хочешь получать сводки?\n\n";

        var times = new[]
            {
            "7:00", "8:00", "9:00",
            "10:00", "11:00", "12:00",
            "13:00", "14:00", "15:00",
            "16:00", "17:00", "18:00",
            "19:00", "20:00", "21:00",
        };

        var keyboard = new List<List<InlineKeyboardButton>>();

        keyboard.Add(new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData("🕒 Во время выхода поста", $"{Consts.NotificationTimeSettingCallBackPrefix}realtime")
        });

        for (int i = 0; i < times.Length; i += 3)
        {
            keyboard.Add(times
                .Skip(i).Take(3)
                .Select(t => InlineKeyboardButton.WithCallbackData(t, $"{Consts.NotificationTimeSettingCallBackPrefix}{t}"))
                .ToList());
        }

        await bot.ReactivelySendAsync(
            chatId,
            text: text,
            replyMarkup: new InlineKeyboardMarkup(keyboard));
    }
}