using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types;
using Telegram.Bot;
using SummyAITelegramBot.Core.Bot.Abstractions;
using SummyAITelegramBot.Core.Domain.Models;
using Telegram.Bot.Types.Enums;

namespace SummyAITelegramBot.Core.Bot.Features.Settings.Handlers;

public class NotificationsSettingsHandler : IChainOfStepsHandler<UserSettings>
{
    public IChainOfStepsHandler<UserSettings>? Next { get; set; }

    private static readonly string CallbackPrefix = "settings:notify:time:";

    public async Task ShowStepAsync(ITelegramBotClient bot, long chatId)
    {
        var text =
            "<b>1️⃣ Начнём с первой настройки</b>\n\n" +
            "⏱️ В какое время ты хочешь получать сводки?\n\n";

        var times = new[]
        {
            "1:00", "2:00", "6:00",
            "7:00", "8:00", "9:00",
            "10:00", "11:00", "12:00",
            "13:00", "14:00", "15:00",
            "16:00", "17:00", "18:00",
            "19:00", "20:00", "21:00",
            "22:00", "23:00", "0:00"
        };

        var keyboard = new List<List<InlineKeyboardButton>>();

        keyboard.Add(new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData("🕒 Во время выхода поста", $"{CallbackPrefix}realtime")
        });

        for (int i = 0; i < times.Length; i += 3)
        {
            keyboard.Add(times
                .Skip(i).Take(3)
                .Select(t => InlineKeyboardButton.WithCallbackData(t, $"{CallbackPrefix}{t}"))
                .ToList());
        }

        await bot.SendMessage(chatId, text: text, parseMode: ParseMode.Html,
            replyMarkup: new InlineKeyboardMarkup(keyboard));
    }

    public async Task HandleAsync(ITelegramBotClient bot, CallbackQuery query, UserSettings settings)
    {
        if (query.Data != null && query.Data.StartsWith(CallbackPrefix))
        {
            var timeStr = query.Data.Substring(CallbackPrefix.Length);

            if (timeStr == "realtime")
            {
                settings.InstantlyNotification = true;
            }
            else if (TimeOnly.TryParse(timeStr, out var time))
            {
                settings.NotificationTime = time;
                settings.InstantlyNotification = false;
            }
            else
            {
                await bot.SendMessage(query.Message.Chat.Id, "❌ Ошибка: неверное время.");
                return;
            }

            if (Next != null)
                await Next.ShowStepAsync(bot, query.Message.Chat.Id);
        }
    }
}