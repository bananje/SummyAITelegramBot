using SummyAITelegramBot.Core.Bot.Abstractions;
using SummyAITelegramBot.Core.Bot.Attributes;
using SummyAITelegramBot.Core.Bot.Extensions;
using SummyAITelegramBot.Core.Bot.Utils;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace SummyAITelegramBot.Core.Bot.Features.Settings.Handlers;

[TelegramUpdateHandler("/showtimezonesettings")]
public class ShowTimeZoneSettingsHandler(ITelegramBotClient bot) : ITelegramUpdateHandler
{
    public async Task HandleAsync(Update update)
    {
        var chatId = update.CallbackQuery.Message.Chat.Id;
        var text =
             "<b>🌍 Выбери свой часовой пояс</b>\n\n" +
             "Это нужно, чтобы присылать уведомления в нужное время.\n\n";

        var timezones = new (string Title, string Id)[]
        {
            ("🇷🇺 Москва (UTC+3)", "Europe/Moscow"),
            ("🇷🇺 Калининград (UTC+2)", "Europe/Kaliningrad"),
            ("🇷🇺 Самара (UTC+4)", "Europe/Samara"),
            ("🇷🇺 Екатеринбург (UTC+5)", "Asia/Yekaterinburg"),
            ("🇷🇺 Омск (UTC+6)", "Asia/Omsk"),
            ("🇷🇺 Красноярск (UTC+7)", "Asia/Krasnoyarsk"),
            ("🇷🇺 Иркутск (UTC+8)", "Asia/Irkutsk"),
            ("🇷🇺 Якутск (UTC+9)", "Asia/Yakutsk"),
            ("🇷🇺 Владивосток (UTC+10)", "Asia/Vladivostok"),
            ("🇷🇺 Магадан (UTC+11)", "Asia/Magadan"),
            ("🇷🇺 Камчатка (UTC+12)", "Asia/Kamchatka"),
        };

        var keyboard = new List<List<InlineKeyboardButton>>();

        for (int i = 0; i < timezones.Length; i += 2)
        {
            keyboard.Add(timezones
                .Skip(i).Take(2)
                .Select(tz => InlineKeyboardButton.WithCallbackData(tz.Title, $"{Consts.TimeZoneSettingCallBackPrefix}{tz.Id}"))
                .ToList());
        }

        await bot.ReactivelySendAsync(
            chatId,
            text: text,
            replyMarkup: new InlineKeyboardMarkup(keyboard));
    }
}