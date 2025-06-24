using Microsoft.EntityFrameworkCore;
using SummyAITelegramBot.Core.Abstractions;
using SummyAITelegramBot.Core.Bot.Abstractions;
using SummyAITelegramBot.Core.Bot.Attributes;
using SummyAITelegramBot.Core.Bot.Extensions;
using SummyAITelegramBot.Core.Bot.Utils;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace SummyAITelegramBot.Core.Bot.Features.Channel.Handlers;

[TelegramUpdateHandler("/showchannelsettings")]
public class ShowChannelSettingsHandler(
    ITelegramBotClient bot,
    IUnitOfWork unitOfWork,
    IStaticImageService imageService) : ITelegramUpdateHandler
{
    public async Task HandleAsync(Update update)
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
                InlineKeyboardButton.WithCallbackData("Персонально настроить", 
                    $"{Consts.ChannelSettingsCallbackPrefix}personal")
            }
        };

        var hasGlobalSetting = user.UserSettings.Any(u => u.IsGlobal);

        if (hasGlobalSetting)
        {
            keyboard.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData("Установить как у всех каналов", 
                    $"{Consts.ChannelSettingsCallbackPrefix}global_apply"),

                InlineKeyboardButton.WithCallbackData("Сбросить общие настройки",
                    $"{Consts.ChannelSettingsCallbackPrefix}global_clear")
            });
        }
        else
        {
            keyboard.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData("Настроить сразу для всех каналов", 
                    $"{Consts.ChannelSettingsCallbackPrefix}global_create"),
            });
        }

        var text = $"""
                2️⃣ <b>Указываем время получения сводок</b>

                {update.Message.From.FirstName}, пожалуйста, добавьте удобное
                для Вас день и время получения сводок🦉
                """;

        await using var stream = imageService.GetImageStream("add_channel.jpg");

        await bot.ReactivelySendPhotoAsync(
                update.Message.Chat.Id,
                photo: stream,
                caption: text,
                update.Message,
                replyMarkup: new InlineKeyboardMarkup(keyboard)
        );
    }
}