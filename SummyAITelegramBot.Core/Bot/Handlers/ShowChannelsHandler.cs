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
    IUnitOfWork unitOfWork) : ITelegramUpdateHandler
{
    private readonly IRepository<long, Domain.Models.User> _userRepository = unitOfWork.Repository<long, Domain.Models.User>();
    private const int PageSize = 5;

    public async Task HandleAsync(Update update)
    {
        var (userId, chatId) = GetUserAndChatId(update);
        var offset = GetOffsetFromUpdate(update); // pagination

        var user = await _userRepository.GetIQueryable()
            .Where(u => u.Id == userId)
            .Include(u => u.Channels)
            .FirstOrDefaultAsync()
                ?? throw new Exception($"Пользователь {userId} не найден.");

        var totalChannels = user.Channels.Count;
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

        if (offset + PageSize < totalChannels)
        {
            buttons.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData("➡️ Далее", $"/mychannels:{offset + PageSize}")
            });
        }

        var text = "<b>📢 Ваши каналы:</b>\n\nНажмите на канал, чтобы выбрать его.";

        var markup = new InlineKeyboardMarkup(buttons);

        if (update.CallbackQuery != null)
        {
            await bot.ReactivelySendAsync(
                chatId,
                text,
                replyMarkup: markup,
                userMessage: update.CallbackQuery.Message
            );
        }
        else
        {
            await bot.ReactivelySendAsync(
                chatId,
                text,
                replyMarkup: markup,
                userMessage: update.Message
            );
        }
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