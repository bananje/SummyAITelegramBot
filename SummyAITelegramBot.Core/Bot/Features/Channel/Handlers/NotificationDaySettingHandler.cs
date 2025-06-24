using Microsoft.Extensions.Caching.Memory;
using SummyAITelegramBot.Core.Abstractions;
using SummyAITelegramBot.Core.Bot.Abstractions;
using SummyAITelegramBot.Core.Domain.Models;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot;
using Telegram.Bot.Types;
using SummyAITelegramBot.Core.Domain.Enums;
using FluentResults;
using SummyAITelegramBot.Core.Bot.Extensions;

namespace SummyAITelegramBot.Core.Bot.Features.Channel.Handlers;

public class NotificationDaySettingHandler(
    ITelegramBotClient bot,
    IUnitOfWork unitOfWork,
    IMemoryCache cache) : IStepOnChainHandler<UserSettings>
{
    public IStepOnChainHandler<UserSettings>? Next { get; set; }

    private static readonly string CallbackPrefix = "add:channel_chain_day_";

    public async Task ShowStepAsync(Update update)
    {
        var chatId = update.CallbackQuery.Message.Chat.Id;
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

        //await bot.SendOrEditMessageAsync(cache, update, 
        //    text, parseMode: ParseMode.Html,
        //    replyMarkup: new InlineKeyboardMarkup(keyboard));
    }

    public async Task<Result> HandleAsync(Update update, UserSettings userSettings)
    {
        var query = update.CallbackQuery;

        if (query.Data != null && query.Data.StartsWith(CallbackPrefix))
        {
            var settingsRepostitory = unitOfWork.Repository<Guid, UserSettings>();
            var channelRepository = unitOfWork.Repository<long, Domain.Models.Channel>();
            var dayOfWeek = query.Data.Substring(CallbackPrefix.Length);

            if (Enum.TryParse<RussianDayOfWeek>(dayOfWeek, out var day))
            {
                userSettings.Day = (int)day;
            }
            else
            {
                //await bot.SendOrEditMessageAsync(
                //    cache,
                //    update,
                //    "❌ Ошибка: неверный день.",
                //    query.Message.Chat.Id,
                //    query.Message.Id, "❌ Ошибка: неверный день.");

                return Result.Fail("");
            }

            if (Next != null)
                await Next.ShowStepAsync(update);

            return Result.Ok();
        }

        return Result.Fail("");
    }
}