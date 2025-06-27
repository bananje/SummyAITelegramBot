using Microsoft.EntityFrameworkCore;
using SummyAITelegramBot.Core.Abstractions;
using SummyAITelegramBot.Core.Bot.Abstractions;
using SummyAITelegramBot.Core.Bot.Attributes;
using SummyAITelegramBot.Core.Bot.Extensions;
using SummyAITelegramBot.Core.Bot.Utils;
using SummyAITelegramBot.Core.Domain.Models;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace SummyAITelegramBot.Core.Bot.Handlers;

[TelegramUpdateHandler("/showchannelsettings")]
public class ShowChannelSettingsHandler(
    ITelegramBotClient bot,
    IUnitOfWork unitOfWork,
    IStaticImageService imageService) : ITelegramUpdateHandler
{
    public async Task HandleAsync(Update update)
    {
        var chatId = update.CallbackQuery.Message.Chat.Id;
        var userRepository = unitOfWork.Repository<long, Domain.Models.User>();
        var userSettingsRepository = unitOfWork.Repository<Guid, ChannelUserSettings>();

        var user = await userRepository.GetIQueryable()
            .Include(u => u.ChannelUserSettings)
            .FirstOrDefaultAsync(u => u.Id == chatId)
                ?? throw new Exception($"Ошибка при настройке пользователя {chatId}.");
        var userSettings = user.ChannelUserSettings;

        var keyboard = new List<List<InlineKeyboardButton>>();
        string text = "";

        if (userSettings is not null)
        {
            keyboard.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData("Применить текущее",
                    $"{Consts.ChannelSettingsCallbackPrefix}apply"),

                InlineKeyboardButton.WithCallbackData("Поменять время",
                    $"{Consts.ChannelSettingsCallbackPrefix}clear-create")
            });

            text = $"""
                2️⃣ <b>Указываем время получения сводок</b>

                Текущее время: {userSettings.NotificationTime} по {userSettings.TimeZoneId}
                для получения сводок🦉
                """;
        }
        else
        {
            keyboard.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData("Настроить",
                    $"{Consts.ChannelSettingsCallbackPrefix}create"),
            });

            text = $"""
                2️⃣ <b>Указываем время получения сводок</b>

                {update.Message.From.FirstName}, пожалуйста, укажите удобное
                для Вас время получения сводок🦉
                """;
        }

        await using var stream = imageService.GetImageStream("add_channel.jpg");

        await bot.ReactivelySendPhotoAsync(
                chatId,
                photo: stream,
                caption: text,
                replyMarkup: new InlineKeyboardMarkup(keyboard)
        );
    }
}