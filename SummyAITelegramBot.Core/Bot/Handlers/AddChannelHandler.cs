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
using SummyAITelegramBot.Core.Bot.Utils;
using SummyAITelegramBot.Core.Domain.Models;

namespace SummyAITelegramBot.Core.Bot.Handlers;

[TelegramUpdateHandler("/add")]
public class AddChannelHandler(
    ITelegramBotClient bot,
    IStaticImageService imageService,
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
        ITelegramChannelAdapter channelAdapter,
        IMemoryCache cache,
        IUnitOfWork unitOfWork)
        : this(bot, imageService, channelAdapter, cache)
    {
        _unitOfWork = unitOfWork;
        _userSettingsRepository = unitOfWork.Repository<Guid, ChannelUserSettings>();
        _userRepository = unitOfWork.Repository<long, UserEn>();
        _channelRepository = unitOfWork.Repository<long, ChannelEn>();
    }

    public async Task HandleAsync(Update update)
    {
        if (update?.Message?.Text == "/add" 
            || update?.CallbackQuery?.Data == "/add")
        {
            await SendWelcomeText(update);
            return;
        }

        var channelLink = update.Message.Text;
        var userId = update.Message.From.Id;
        var userRepository = _unitOfWork.Repository<long, UserEn>();

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

        var user = await userRepository.GetIQueryable()
            .Include(u => u.Channels)
            .FirstOrDefaultAsync(user => user.Id == userId)
                ?? throw new Exception($"Ошибка при настройке пользователя {userId}.");

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
        var chatId = update.Message.Chat.Id;
        string btnCommand = "/showchannelsettings";

        
        var currentChannels = await _channelRepository.GetIQueryable()
            .Where(u => u.user)
            .Select(u => u.Link)
            .ToListAsync();

        if (currentChannels.Count > channelsCountBeforeAdding) { btnCommand = "/showchannelsettings"; }
        else { btnCommand = "/complete"; }

       var keyboard = new InlineKeyboardMarkup(new[]
       {
           new[] { InlineKeyboardButton.WithCallbackData("Завершить добавление➡️", $"{btnCommand}") }
       });

        var text = $"""
            {caption}

            Добавлено каналов: 

            {string.Join(Environment.NewLine, currentChannels.Select(link => $"📢 {link}"))}

            Отправьте ссылку на другой ваш канал или нажмите (Завершить добавление➡️)

            <b> *В базовом тарифе можно добавить до 5 каналов 📢</b>
            """;

        await using var stream = imageService.GetImageStream("add_channel.jpg");

        await bot.ReactivelySendPhotoAsync(
            chatId,
            photo: new InputFileStream(stream),
            userMessage: update.Message,
            caption: text,
            replyMarkup: keyboard
        );
    }

    private async Task SendWelcomeText(Update update)
    {
        var message = update.Message is null 
            ? update.CallbackQuery.Message
            : update.Message;

        var text = $"""
                1️⃣ <b>Добавьте Ваши каналы</b>

                Просто отправьте ссылку на канал
                (Пример: https://t.me/UseSummyAI)

                <b> *В базовом тарифе можно добавить до 5 каналов 📢</b>
                """;

        await using var stream = imageService.GetImageStream("add_channel.jpg");

        await bot.ReactivelySendPhotoAsync(
            message.Chat.Id,
            photo: new InputFileStream(stream),
            userMessage: update.Message,
            caption: text
        );
    }
}