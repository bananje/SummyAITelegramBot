using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types;
using Telegram.Bot;
using SummyAITelegramBot.Core.Bot.Abstractions;
using Telegram.Bot.Types.Enums;
using SummyAITelegramBot.Core.Abstractions;
using SummyAITelegramBot.Core.Domain.Models;
using Microsoft.Extensions.Caching.Memory;
using FluentResults;

namespace SummyAITelegramBot.Core.Bot.Features.Settings.Handlers;

public class NotificationTimeSettingHandler(
    ITelegramBotClient bot,
    IUnitOfWork unitOfWork) : IStepOnChainHandler<UserSettings>
{
    public IStepOnChainHandler<UserSettings>? Next { get; set; }

    private static readonly string CallbackPrefix = "add:channel_chain_time_";

    public async Task ShowStepAsync(Update update)
    {
        var chatId = update.CallbackQuery.Message.Chat.Id;
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

        await bot.EditMessageText(chatId, update.CallbackQuery.Message.Id, 
            text: text, parseMode: ParseMode.Html,
            replyMarkup: new InlineKeyboardMarkup(keyboard));
    }

    public async Task<Result> HandleAsync(Update update, UserSettings userSettings)
    {
        var query = update.CallbackQuery;

        if (query.Data != null && query.Data.StartsWith(CallbackPrefix))
        {
            var settingsRepostitory = unitOfWork.Repository<Guid, UserSettings>();
            var channelRepository = unitOfWork.Repository<long, Domain.Models.Channel>();
            var timeStr = query.Data.Substring(CallbackPrefix.Length);

            if (timeStr == "realtime")
            {
                userSettings.InstantlyTimeNotification = true;
            }
            else if (TimeOnly.TryParse(timeStr, out var time))
            {
                userSettings.NotificationTime = time;
                userSettings.InstantlyTimeNotification = false;
            }
            else
            {
                await bot.EditMessageText(
                    query.Message.Chat.Id,
                    query.Message.Id, "❌ Ошибка: неверное время.");

                return Result.Fail("");
            }

            if (Next != null)
                await Next.ShowStepAsync(update);

            return Result.Ok();
        }

        return Result.Fail("");
    }
}