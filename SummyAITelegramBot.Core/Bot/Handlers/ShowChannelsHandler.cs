using SummyAITelegramBot.Core.Abstractions;
using SummyAITelegramBot.Core.Bot.Abstractions;
using SummyAITelegramBot.Core.Bot.Attributes;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot;
using Telegram.Bot.Types;
using Microsoft.EntityFrameworkCore;
using SummyAITelegramBot.Core.Bot.Extensions;

namespace SummyAITelegramBot.Core.Bot.Handlers;

[TelegramUpdateHandler("/mychannels")]
public class ShowChannelsPaginatedHandler(
    ITelegramBotClient bot,
    IStaticImageService staticImageService,
    IUnitOfWork unitOfWork) : ITelegramUpdateHandler
{
    private readonly IRepository<long, Domain.Models.User> _userRepository = unitOfWork.Repository<long, Domain.Models.User>();
    private const int PageSize = 5;

    public async Task HandleAsync(Update update)
    {
        var (userId, chatId) = GetUserAndChatId(update);
        var offset = GetOffsetFromUpdate(update);

        var user = await _userRepository.GetIQueryable()
            .Where(u => u.Id == userId)
            .Include(u => u.Channels)
            .FirstOrDefaultAsync()
                ?? throw new Exception($"Пользователь {userId} не найден.");

        var totalChannels = user.Channels.Count;

        if (totalChannels == 0)
        {
            var noChannelsText = """
            ❗️ <b>У вас пока нет добавленных каналов.</b>

            Чтобы начать получать сводки, добавьте хотя бы один канал.
            """;

            var addChannelButton = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("➕ Добавить канал", "/add") }
            });

            await using var emptyStream = staticImageService.GetImageStream("summy_delete.jpg");

            await bot.ReactivelySendPhotoAsync(
                chatId,
                new InputFileStream(emptyStream),
                noChannelsText,
                replyMarkup: addChannelButton,
                userMessage: update.CallbackQuery?.Message ?? update.Message
            );
            return;
        }

        var paginatedChannels = user.Channels
            .OrderBy(c => c.Link)
            .Skip(offset)
            .Take(PageSize)
            .ToList();

        var buttons = paginatedChannels
            .Select(c => new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData(c.Title ?? c.Link, $"/deletechannel:{c.Id}")
            })
            .ToList();

        // Кнопки навигации
        var hasPrev = offset > 0;
        var hasNext = offset + PageSize < totalChannels;

        if (hasPrev && hasNext)
        {
            buttons.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData("🦉 Личный кабинет", "/account"),
                InlineKeyboardButton.WithCallbackData("⬅️ Назад", $"/mychannels:{offset - PageSize}"),
                InlineKeyboardButton.WithCallbackData("➡️ Далее", $"/mychannels:{offset + PageSize}")
            });
        }
        else if (hasPrev)
        {
            buttons.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData("🦉 Личный кабинет", "/account"),
                InlineKeyboardButton.WithCallbackData("⬅️ Назад", $"/mychannels:{offset - PageSize}")
            });
        }
        else if (hasNext)
        {
            buttons.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData("🦉 Личный кабинет", "/account"),
                InlineKeyboardButton.WithCallbackData("➡️ Далее", $"/mychannels:{offset + PageSize}")
            });
        }
        else
        {
            buttons.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData("🦉 Личный кабинет", "/account")
            });
        }

        var text = "<b>📢 Ваши каналы:</b>\n\nНажмите на канал, чтобы удалить его.";
        var markup = new InlineKeyboardMarkup(buttons);

        await using var stream = staticImageService.GetImageStream("summy_delete.jpg");

        await bot.ReactivelySendPhotoAsync(
            chatId,
            caption: text,
            photo: new InputFileStream(stream),
            replyMarkup: markup,
            userMessage: update.CallbackQuery?.Message ?? update.Message
        );
    }

    private (long userId, long chatId) GetUserAndChatId(Update update)
    {
        if (update.Message != null)
            return (update.Message.From.Id, update.Message.Chat.Id);

        if (update.CallbackQuery != null)
            return (update.CallbackQuery.From.Id, update.CallbackQuery.Message.Chat.Id);

        throw new Exception("Неизвестный формат обновления");
    }

    private int GetOffsetFromUpdate(Update update)
    {
        var data = update.CallbackQuery?.Data;
        if (data != null && data.StartsWith("/mychannels:"))
        {
            var offsetStr = data.Replace("/mychannels:", "");
            if (int.TryParse(offsetStr, out var offset))
                return offset;
        }
        return 0;
    }
}