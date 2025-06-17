using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types;
using SummyAITelegramBot.Core.Bot.Abstractions;
using SummyAITelegramBot.Core.Domain.Models;
using SummyAITelegramBot.Core.Domain.Enums;

namespace SummyAITelegramBot.Core.Bot.Features.Settings.Handlers;

//public class LanguageSettingsStepHandler(
//    ITelegramBotClient bot) : IStepOnChainHandler<UserSettings, CallbackQuery>
//{
//    public IStepOnChainHandler<UserSettings, CallbackQuery>? Next { get; set; }

//    public async Task ShowStepAsync(long chatId)
//    {
//        var keyboard = new InlineKeyboardMarkup(new[]
//        {
//            InlineKeyboardButton.WithCallbackData("Русский", "settings:lang:ru"),
//            InlineKeyboardButton.WithCallbackData("English", "settings:lang:en")
//        });

//        await bot.SendMessage(chatId, "Выберите язык:", replyMarkup: keyboard);
//    }

//    public async Task HandleAsync(CallbackQuery query, UserSettings settings)
//    {
//        if (query.Data == "settings:lang:ru")
//            settings.Language = Lanquage.RU;
//        else if (query.Data == "settings:lang:en")
//            settings.Language = Lanquage.EN;

//        if (Next != null)
//            await Next.ShowStepAsync(query.Message.Chat.Id);
//    }
//}