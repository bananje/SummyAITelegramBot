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

namespace SummyAITelegramBot.Core.Bot.Features.Channel.Handlers;

[TelegramUpdateHandler("/channellink")]
public class AddChannelHandler(
    IUnitOfWork unitOfWork,
    ITelegramBotClient bot,
    ITelegramUpdateFactory telegramUpdateFactory,
    IStaticImageService imageService,
    ITelegramChannelAdapter channelAdapter,
    IMemoryCache cache) : ITelegramUpdateHandler
{
    public async Task HandleAsync(Update update)
    {
        var channelLink = update.Message.Text;
        var userId = update.Message.From.Id;
        var channelRepository = unitOfWork.Repository<long, ChannelEn>();
        var userRepository = unitOfWork.Repository<long, UserEn>();

        var channel = new ChannelEn();
        TL.Channel? channelInfo = default;
        try
        {
            channelInfo = await channelAdapter.ResolveChannelAsync(channelLink);

            if (await channelRepository.GetByIdAsync(channelInfo.id) is null)
            {
                channel = new ChannelEn
                {
                    HasStopFactor = channelInfo!.flags.HasFlag(TL.Channel.Flags.fake)
                        || channelInfo.flags.HasFlag(TL.Channel.Flags.scam),
                    Link = channelLink,
                    Id = channelInfo.id,
                };

                await channelRepository.AddAsync(channel);
            }
            else
            {
                channel = await channelRepository.GetByIdAsync(channelInfo.id);
            }
        }
        catch (Exception ex)
        {
            var text = $"""
                ⚠️ <b>Не могу найти такой канал</b>

                Пожалуйста, проверьте ссылку и отправьте снова.
                (Пример: https://t.me/UseSummyAI)
                """;

            await using var failStream = imageService.GetImageStream("add_channel.jpg");
            await bot.ReactivelySendPhotoAsync(
                chatId: update.Message.Chat.Id,
                photo: new InputFileStream(failStream),
                caption: text,
                userMessage: update.Message);
        }

        var user = await userRepository.GetIQueryable()
            .Include(u => u.Channels)
            .FirstOrDefaultAsync(user => user.Id == userId)
                ?? throw new Exception($"Ошибка при настройке пользователя {userId}.");

        if (user.Channels.Any(u => u.Id == channelInfo.id))
        {
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                InlineKeyboardButton.WithCallbackData("➕ Канал", "/add"),
                InlineKeyboardButton.WithCallbackData("Завершить добавление", "/settings")
            });

            var text = $"""
                ✅ <b>Канал уже добавлен в библиотеку</b>

                *Нажмите ➕ Канал, чтобы добавить новый канал
                """;

            await using var failStream = imageService.GetImageStream("add_channel.jpg");
            await bot.ReactivelySendPhotoAsync(
                update.Message.Chat.Id,
                photo: new InputFileStream(failStream),
                caption: text,
                userMessage: update.Message);
        }

        user.AddChannel(channel);
        await unitOfWork.CommitAsync();

        var userSettings = new UserSettings
        { 
            UserId = userId,
            ChannelId = channel.Id
        };

        cache.Set($"{Consts.UserSettingsCachePrefix}{userId}", userSettings, TimeSpan.FromMinutes(5));

        await telegramUpdateFactory.DispatchAsync(update, "/showchannelsettings");
    }
}