using SummyAITelegramBot.Core.Bot.Abstractions;
using SummyAITelegramBot.Core.Domain.Models;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace SummyAITelegramBot.Core.Bot.Features.Settings.Handlers;

//public class GlobalSettingsStepHadler(
//    ITelegramBotClient bot) 
//    : IStepOnChainHandler<UserSettings, CallbackQuery>
//{
//    public IStepOnChainHandler<UserSettings, CallbackQuery>? Next { get; set; }

//    public async Task HandleAsync(CallbackQuery query, UserSettings entity)
//    {
//        if (query.Data == "settings:global:yes")
//            entity.IsGlobal = true;
//        else if (query.Data == "settings:global:no")
//            entity.IsGlobal = false;

//        if (Next != null)
//            await Next.ShowStepAsync(query);
//    }

//    public async Task ShowStepAsync(CallbackQuery query)
//    {
//        var chatId = query.Message.Chat.Id;
//        var keyboard = new InlineKeyboardMarkup(new[]
//        {
//            InlineKeyboardButton.WithCallbackData("Да", "settings:global:yes"),
//            InlineKeyboardButton.WithCallbackData("Нет", "settings:global:no")
//        });

//        await bot.SendMessage(chatId, "Применить настройку для всех каналов?", replyMarkup: keyboard);
//    }

//    public Task ShowStepAsync(long chatId)
//    {
//        throw new NotImplementedException();
//    }
//}