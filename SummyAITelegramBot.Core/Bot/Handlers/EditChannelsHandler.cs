using SummyAITelegramBot.Core.Abstractions;
using SummyAITelegramBot.Core.Bot.Abstractions;
using SummyAITelegramBot.Core.Bot.Attributes;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot;
using Telegram.Bot.Types;
using SummyAITelegramBot.Core.Bot.Extensions;
using Microsoft.EntityFrameworkCore;

[TelegramUpdateHandler("/showchannels")]
public class ShowChannelsHandler(
    ITelegramBotClient bot,
    IStaticImageService imageService,
    ITelegramUpdateFactory telegramUpdateFactory,
    IUnitOfWork unitOfWork) : ITelegramUpdateHandler
{
    private readonly IRepository<long, SummyAITelegramBot.Core.Domain.Models.User> _userRepository 
        = unitOfWork.Repository<long, SummyAITelegramBot.Core.Domain.Models.User>();
    private const int PageSize = 5;

    public async Task HandleAsync(Update update)
    {
        var (userId, chatId) = GetUserAndChatId(update);
        var limit = GetLimitFromUpdate(update);

        var user = await _userRepository.GetIQueryable()
            .Where(u => u.Id == userId)
            .Include(u => u.Channels)
            .FirstOrDefaultAsync()
                ?? throw new Exception($"Пользователь {userId} не найден.");

        var channels = user.Channels
            .OrderBy(c => c.Link)
            .Take(limit)
            .ToList();

        var hasMore = user.Channels.Count > limit;

                var text = $"""
        📋 <b>Ваши каналы:</b>       

        {string.Join("\n", channels.Select(c => $"📢 {c.Link}"))}

        <b>*Для удаления из списка напишите команду /remove и укажите ссылку 
        
        Пример (/remove https://t.me/UseSummyAI)</b>
        """;

        var buttons = new List<List<InlineKeyboardButton>>();

        if (hasMore)
        {
            buttons.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData("🔽 Показать ещё", $"/showchannels:{limit + PageSize}")
            });
        }

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
        {
            return (update.Message.From.Id, update.Message.Chat.Id);
        }

        if (update.CallbackQuery != null)
        {
            return (update.CallbackQuery.From.Id, update.CallbackQuery.Message.Chat.Id);
        }

        throw new Exception("Неизвестный формат обновления");
    }

    private int GetLimitFromUpdate(Update update)
    {
        var data = update.CallbackQuery?.Data;
        if (data != null && data.StartsWith("/showchannels:"))
        {
            var limitStr = data.Replace("/showchannels:", "");
            if (int.TryParse(limitStr, out var limit))
            {
                return limit;
            }
        }
        return PageSize;
    }
}