using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types;
using Telegram.Bot;
using SummyAITelegramBot.Core.Bot.Abstractions;
using SummyAITelegramBot.Core.Domain.Models;

namespace SummyAITelegramBot.Core.Bot.Features.Settings.Handlers;

public class NotificationsSettingsHandler : IChainOfStepsHandler<UserSettings>
{
    public IChainOfStepsHandler<UserSettings>? Next { get; set; }

    public async Task ShowStepAsync(ITelegramBotClient bot, long chatId)
    {
        var keyboard = new InlineKeyboardMarkup(new[]
        {
            InlineKeyboardButton.WithCallbackData("Вкл.", "settings:notify:on"),
            InlineKeyboardButton.WithCallbackData("Выкл.", "settings:notify:off")
        });

        await bot.SendMessage(chatId, "Получать уведомления?", replyMarkup: keyboard);
    }

    public async Task HandleAsync(ITelegramBotClient bot, CallbackQuery query, UserSettings settings)
    {
        if (query.Data == "settings:notify:on")
            settings.NotificationsEnabled = true;
        else if (query.Data == "settings:notify:off")
            settings.NotificationsEnabled = false;

        if (Next != null)
            await Next.ShowStepAsync(bot, query.Message.Chat.Id);
    }
}