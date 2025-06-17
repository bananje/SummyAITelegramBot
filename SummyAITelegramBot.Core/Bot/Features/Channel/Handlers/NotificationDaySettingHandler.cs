using Microsoft.Extensions.Caching.Memory;
using SummyAITelegramBot.Core.Abstractions;
using SummyAITelegramBot.Core.Bot.Abstractions;
using SummyAITelegramBot.Core.Domain.Models;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot;
using Telegram.Bot.Types;
using SummyAITelegramBot.Core.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace SummyAITelegramBot.Core.Bot.Features.Channel.Handlers;

public class NotificationDaySettingHandler(
    ITelegramBotClient bot,
    IUnitOfWork unitOfWork) : IStepOnChainHandler
{
    public IStepOnChainHandler? Next { get; set; }

    private static readonly string CallbackPrefix = "add_channel_chain_day:";

    public async Task ShowStepAsync(Update update)
    {
        var chatId = update.Message.Chat.Id;
        var text =
             "<b>1️⃣ Начнём с первой настройки</b>\n\n" +
             "⏱️ В какое время ты хочешь получать сводки?\n\n";

        var times = new[]
        {
            "ПН", "ВТ", "СР",
            "ЧТ", "ПТ", "СБ",
            "ВС", "Ежедневно"
        };

        var keyboard = new List<List<InlineKeyboardButton>>();

        //keyboard.Add(new List<InlineKeyboardButton>
        //{
        //    InlineKeyboardButton.WithCallbackData("🕒 Во время выхода поста", $"{CallbackPrefix}realtime")
        //});

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
            var dayOfWeek = query.Data.Substring(CallbackPrefix.Length);
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

            if (Enum.TryParse<RussianDayOfWeek>(dayOfWeek, out var day))
            {
                settings.Day = (int)day;
            }
            else
            {
                await bot.SendMessage(query.Message.Chat.Id, "❌ Ошибка: неверный день.");
                return;
            }

            await settingsRepostitory.CreateOrUpdateAsync(settings);
            await unitOfWork.CommitAsync();

            if (Next != null)
                await Next.ShowStepAsync(update);
        }
    }
}