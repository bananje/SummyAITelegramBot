using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types;
using Telegram.Bot;
using SummyAITelegramBot.Core.Bot.Abstractions;
using Telegram.Bot.Types.Enums;
using SummyAITelegramBot.Core.Abstractions;
using SummyAITelegramBot.Core.Domain.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;

namespace SummyAITelegramBot.Core.Bot.Features.Settings.Handlers;

public class NotificationTimeSettingHandler(
    ITelegramBotClient bot,
    IUnitOfWork unitOfWork,
    IMemoryCache cache) : IStepOnChainHandler
{
    public IStepOnChainHandler? Next { get; set; }

    private static readonly string CallbackPrefix = "add_channel_chain_time:";

    public async Task ShowStepAsync(Update update)
    {
        var chatId = update.Message.Chat.Id;
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

    public async Task HandleAsync(Update update)
    {
        var query = update.CallbackQuery;

        if (query.Data != null && query.Data.StartsWith(CallbackPrefix))
        {
            var settingsRepostitory = unitOfWork.Repository<Guid, UserSettings>();
            var channelRepository = unitOfWork.Repository<long, Domain.Models.Channel>();
            var timeStr = query.Data.Substring(CallbackPrefix.Length);

            long? channelId = await channelRepository.GetIQueryable()
                .OrderByDescending(u => u.AddedDate)
                .Select(u => u.Id)
                .FirstOrDefaultAsync();

            if (channelId is null) { throw new Exception("Ошибка. Канал не найден"); }

            var settings = new UserSettings
            {
                ChannelId = channelId.Value,
                UserId = update.Message.From.Id
            };

            if (timeStr == "realtime")
            {
                settings.InstantlyTimeNotification = true;
            }
            else if (TimeOnly.TryParse(timeStr, out var time))
            {
                settings.NotificationTime = time;
                settings.InstantlyTimeNotification = false;
            }
            else
            {
                await bot.SendMessage(query.Message.Chat.Id, "❌ Ошибка: неверное время.");
                return;
            }

            await settingsRepostitory.CreateOrUpdateAsync(settings);
            await unitOfWork.CommitAsync();

            if (Next != null)
                await Next.ShowStepAsync(update);
        }
    }
}