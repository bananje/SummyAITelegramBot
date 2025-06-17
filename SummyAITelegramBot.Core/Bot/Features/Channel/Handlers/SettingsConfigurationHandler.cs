using Microsoft.EntityFrameworkCore;
using SummyAITelegramBot.Core.Abstractions;
using SummyAITelegramBot.Core.Bot.Abstractions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace SummyAITelegramBot.Core.Bot.Features.Channel.Handlers;

public class SettingsConfigurationHandler(
    ITelegramBotClient bot,
    IUnitOfWork unitOfWork) : IStepOnChainHandler
{
    public IStepOnChainHandler? Next { get; set; }

    private static readonly string CallbackPrefix = "add_channel_chain_settings_config:";

    public Task HandleAsync(Update update)
    {
        throw new NotImplementedException();
    }

    public async Task ShowStepAsync(Update update)
    {
        var userId = update.Message.From!.Id;
        var userRepository = unitOfWork.Repository<long, Domain.Models.User>();

        var user = await userRepository.GetIQueryable()
            .Include(u => u.UserSettings)
            .FirstOrDefaultAsync(u => u.Id == userId) 
                ?? throw new Exception($"Ошибка при настройке пользователя {userId}.");

        var keyboard = new List<List<InlineKeyboardButton>>
        {
            new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData("Персонально настроить", $"{CallbackPrefix}personal")
            }
        };

        var hasGlobalSetting = user.UserSettings.Any(u => u.IsGlobal);

        if (hasGlobalSetting)
        {
            keyboard.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData("Применить глобальные настройки", $"{CallbackPrefix}global_apply"),
                InlineKeyboardButton.WithCallbackData("Сбросить глобальные настройки", $"{CallbackPrefix}global_clear")
            });
        }
        else
        {
            keyboard.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData("Настроить сразу для всех каналов", $"{CallbackPrefix}global_create"),
            });
        }

        await bot.SendMessage(
                update.Message.Chat.Id,
                "Отправьте ссылку на канал:",
                replyMarkup: new ForceReplyMarkup { Selective = true }
        );
    }
}