using SummyAITelegramBot.Core.Abstractions;
using SummyAITelegramBot.Core.Bot.Abstractions;
using ChannelEn = SummyAITelegramBot.Core.Domain.Models.Channel;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Microsoft.EntityFrameworkCore;
using SummyAITelegramBot.Core.Bot.Features.Channel.Abstractions;
using UserEn = SummyAITelegramBot.Core.Domain.Models.User;
using Microsoft.Extensions.Caching.Memory;
using SummyAITelegramBot.Core.Bot.Attributes;
using SummyAITelegramBot.Core.Bot.Extensions;
using SummyAITelegramBot.Core.Domain.Models;
using SummyAITelegramBot.Core.Bot.Utils;
using System.Collections.Generic;

namespace SummyAITelegramBot.Core.Bot.Features.Channel.Handlers;

[TelegramUpdateHandler("/add")]
public class AddChannelHandler(
    ITelegramBotClient bot,
    IStaticImageService imageService,
    IUserCommandCache commandCache,
    ITelegramUpdateFactory telegramUpdateFactory,
    ITelegramChannelAdapter channelAdapter,
    IMemoryCache cache) : ITelegramUpdateHandler
{
    private readonly IRepository<Guid, ChannelUserSettings> _userSettingsRepository;
    private readonly IRepository<long, ChannelEn> _channelRepository;
    private readonly IRepository<long, UserEn> _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private int channelsCountBeforeAdding = 0;

    public AddChannelHandler(
        ITelegramBotClient bot,
        IStaticImageService imageService,
        IUserCommandCache commandCache,
        ITelegramUpdateFactory telegramUpdateFactory,
        ITelegramChannelAdapter channelAdapter,
        IMemoryCache cache,
        IUnitOfWork unitOfWork)
        : this(bot, imageService, commandCache, telegramUpdateFactory, channelAdapter, cache)
    {

        _unitOfWork = unitOfWork;
        _userSettingsRepository = unitOfWork.Repository<Guid, ChannelUserSettings>();
        _userRepository = unitOfWork.Repository<long, UserEn>();
        _channelRepository = unitOfWork.Repository<long, ChannelEn>();
    }

    public async Task HandleAsync(Update update)
    {
        var commands = new List<string>() { "/add", "/start" };

        if (update.CallbackQuery?.Data.StartsWith("/add:") == true)
        {
            var channelsText = $"""
                ✅ <b>Ваши каналы</b>
                """;

            await SendAddedChannelEventMessageAsync(update, channelsText);
            return;
        }

        if (commands.Contains(update?.Message?.Text)
            || commands.Contains(update?.CallbackQuery?.Data))
        {
            await SendWelcomeText(update);
            return;
        }

        var userInfo = TelegramHelper.GetUserAndChatId(update);
        var userId = userInfo.userId;

        var user = await _userRepository.GetIQueryable()
            .Include(u => u.Channels)
            .FirstOrDefaultAsync(user => user.Id == userId)
                ?? throw new Exception($"Ошибка при настройке пользователя {userId}.");

        if (user.Channels.Count == 5 && !user.HasSubscriptionPremium)
        {
            commandCache.SetLastCommand(userId, "/add");
            await telegramUpdateFactory.DispatchAsync(update, "/showsubscription");

            return;
        }

        var channelLink = update.Message.Text;
        channelsCountBeforeAdding = await _channelRepository.GetIQueryable().CountAsync();

        var channel = new ChannelEn();
        TL.Channel? channelInfo = default;
        try
        {
            channelInfo = await channelAdapter.ResolveChannelAsync(channelLink);

            if (await _channelRepository.GetByIdAsync(channelInfo.id) is null)
            {
                channel = new ChannelEn
                {
                    HasStopFactor = channelInfo!.flags.HasFlag(TL.Channel.Flags.fake)
                        || channelInfo.flags.HasFlag(TL.Channel.Flags.scam),
                    Link = channelLink,
                    Id = channelInfo.id,
                    Title = channelInfo.title
                };

                await _channelRepository.AddAsync(channel);
            }
            else
            {
                channel = await _channelRepository.GetByIdAsync(channelInfo.id);
            }
        }
        catch (Exception ex)
        {
            var text = $"""
                ⚠️ <b>Не могу найти такой канал</b>
                """;

            await SendAddedChannelEventMessageAsync(update, text);
            return;
        }

        if (user.Channels.Any(u => u.Id == channelInfo.id))
        {
            var text = $"""
                ✅ <b>Канал уже добавлен в библиотеку</b>
                """;

            await SendAddedChannelEventMessageAsync(update, text);
            return;
        }

        user.AddChannel(channel);
        await _unitOfWork.CommitAsync();

        var completeHeader = $"""
                ✅ <b>Канал успешно добавлен</b>
                """;

        await SendAddedChannelEventMessageAsync(update, completeHeader);
    }

    private async Task SendAddedChannelEventMessageAsync(Update update, string caption)
    {
        var chatInfo = TelegramHelper.GetUserAndChatId(update);
        var chatId = chatInfo.chatId;
        var limit = GetLimitFromUpdate(update);

        var user = await _userRepository.GetIQueryable()
            .Where(u => u.Id == chatId)
            .Include(u => u.Channels)
            .FirstOrDefaultAsync();

        if (user is null)
            throw new Exception($"Пользователь {chatId} не найден.");

        var channels = user.Channels
            .OrderBy(c => c.Link)
            .Take(limit)
            .ToList();

        var hasMore = user.Channels.Count > limit;

        var buttons = new List<List<InlineKeyboardButton>>
    {
        new() { InlineKeyboardButton.WithCallbackData("Завершить добавление➡️", "/showchannelsettings") }
    };

        if (channels.Any())
        {
            if (hasMore)
            {
                buttons.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData("🔽 Показать ещё каналы", $"/add:{limit + 5}")
            });
            }
        }

        var keyboard = new InlineKeyboardMarkup(buttons);

        var channelsText = channels.Any()
            ? $"""
               Добавлено каналов:

               {string.Join(Environment.NewLine, channels.Select(ch => $"📢 <a href=\"https://t.me/{ExtractUsername(ch.Link)}\">{ch.Title}</a>"))}
               ...
               """
            : "Каналов пока нет.";

        var text = $"""
            {caption}

            {channelsText}

            Отправьте ссылку на другой ваш канал или нажмите (Завершить добавление➡️)

            <b> *В базовом тарифе можно добавить до 5 каналов 📢</b>
            """;

        var stream = imageService.GetImageStream("summy_complete.jpg");

        await bot.ReactivelySendPhotoAsync(
            chatId,
            photo: stream,
            userMessage: update.Message,
            caption: text,
            replyMarkup: keyboard
        );
    }

    private string ExtractUsername(string link)
    {
        // Если ссылка — полная, например https://t.me/username
        // просто извлечь часть после последнего слеша
        if (Uri.TryCreate(link, UriKind.Absolute, out var uri))
        {
            return uri.Segments.Last().TrimEnd('/');
        }
        // Иначе вернуть как есть (на случай, если уже username)
        return link.TrimStart('@');
    }

    private string ExtractDisplayName(string link)
    {
        // Если хочешь показывать просто username без @
        var username = ExtractUsername(link);
        return username;
    }

    private int GetLimitFromUpdate(Update update)
    {
        var data = update.CallbackQuery?.Data ?? update.Message?.Text;
        if (data != null && data.StartsWith("/add:"))
        {
            var limitStr = data.Replace("/add:", "");
            if (int.TryParse(limitStr, out var limit))
            {
                return limit;
            }
        }
        return 5; // Default page size
    }

    private async Task SendWelcomeText(Update update)
    {
        var message = update.Message is null
            ? update.CallbackQuery.Message
            : update.Message;


        var buttons = new List<List<InlineKeyboardButton>>
        {
             new() { InlineKeyboardButton.WithCallbackData("🦉 Личный кабинет", "/account") }
        };

        var keyboard = new InlineKeyboardMarkup(buttons);

        var text = $"""
                1️⃣ <b>Добавьте Ваши каналы</b>

                Просто отправьте ссылку на канал
                (Пример: https://t.me/UseSummyAI)

                <b> *В базовом тарифе можно добавить до 5 каналов 📢</b>
                """;

        var stream = imageService.GetImageStream("summy_settings.jpg");

        await bot.ReactivelySendPhotoAsync(
            message.Chat.Id,
            replyMarkup: keyboard,
            photo: stream,
            userMessage: update.Message,
            caption: text
        );
    }
}