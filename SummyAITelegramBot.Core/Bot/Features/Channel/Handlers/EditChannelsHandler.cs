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

        var channelsText = channels.Any()
            ? $"""
            📋 <b>Ваши каналы:</b> 
            
            Добавлено каналов:
            
            {string.Join(Environment.NewLine, channels.Select(ch => $"📢 {ch.Link}").Take(5))}
            ...
            """
            : """
            📋 <b>Вы ещё не добавляли каналы.</b> 
            
            Чтобы добавить канал нажмите Канал📣
            """;

        var text = $"""
            {channelsText}

            Отправьте ссылку на другой ваш канал или нажмите (Завершить добавление➡️)

            <b> *В базовом тарифе можно добавить до 5 каналов 📢</b>
            """;

        var buttons = new List<List<InlineKeyboardButton>>()
        {
            new List<InlineKeyboardButton> { InlineKeyboardButton.WithCallbackData("📣 Канал", $"/add") }
        };


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