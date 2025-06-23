using SummyAITelegramBot.Core.Abstractions;
using SummyAITelegramBot.Core.Bot.Abstractions;
using ChannelEn = SummyAITelegramBot.Core.Domain.Models.Channel;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Microsoft.EntityFrameworkCore;
using SummyAITelegramBot.Core.Bot.Features.Channel.Abstractions;
using UserEn = SummyAITelegramBot.Core.Domain.Models.User;
using SummyAITelegramBot.Core.Domain.Models;
using FluentResults;
using SummyAITelegramBot.Core.Enums;
using SummyAITelegramBot.Core.Extensions;
using Microsoft.Extensions.Caching.Memory;
using SummyAITelegramBot.Core.Bot.Extensions;
using Telegram.Bot.Types.Enums;

namespace SummyAITelegramBot.Core.Bot.Features.Channel.Handlers;

public class AddChannelHandler(
    IUnitOfWork unitOfWork,
    ITelegramBotClient bot,
    IStaticImageService imageService,
    ITelegramChannelAdapter channelAdapter,
    IMemoryCache cache) : IStepOnChainHandler<UserSettings>
{
    public IStepOnChainHandler<UserSettings>? Next { get; set; }

    public async Task<Result> HandleAsync(Update update, UserSettings userSettings)
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
            await bot.SendOrEditMessageAsync(
                cache,
                update,
                photo: new InputFileStream(failStream),
                caption: text,
                parseMode: ParseMode.Html);

            return Result.Ok().WithReason(new Error($"Канал не найден. Внутренее исключение: {ex.Message}"));
        }

        var user = await userRepository.GetIQueryable()
            .Include(u => u.Channels)
            .FirstOrDefaultAsync(user => user.Id == userId)
                ?? throw new Exception($"Ошибка при настройке пользователя {userId}.");

        if (user.Channels.Any(u => u.Id == channelInfo.id))
        {
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                InlineKeyboardButton.WithCallbackData("Добавить канал", "/add"),
                InlineKeyboardButton.WithCallbackData("Завершить добавление", "/settings")
            });

            await bot.SendOrEditMessageAsync(
                cache,
                update,
                "Этот канал уже добавлен в вашу коллекцию:",
                replyMarkup: keyboard);

            return Result.Fail(new ErrorWithCode(ErrorCode.ChannelAlreadyExists));
        }

        user.AddChannel(channel);

        await unitOfWork.CommitAsync();

        userSettings.ChannelId = channel.Id;

        if (Next != null)
            await Next.ShowStepAsync(update);

        return Result.Ok();
    }

    public async Task ShowStepAsync(Update update)
    {
        var message = update.Message is null ? update.CallbackQuery.Message
            : update.Message;

        var userId = message.Chat.Id;
        var userRepository = unitOfWork.Repository<long, Domain.Models.User>();

        var user = await userRepository.GetByIdAsync(userId)
            ?? throw new Exception($"Ошибка при настройке пользователя {userId}.");
        string text = "";

        if (!user.HasSubscriptionPremium && user.Channels.Count > 5) 
        {
            await bot.SendOrEditMessageAsync(
                cache,
                update,
                "Оплатите премиум"
            );
        }
        
        text = $"""
                1️⃣ <b>Добавьте Ваши каналы</b>

                Просто отправьте ссылку на канал
                (Пример: https://t.me/UseSummyAI)

                <b> *В базовом тарифе можно добавить до 5 каналов 📢</b>
                """;

        await using var stream = imageService.GetImageStream("add_channel.jpg");

        await bot.SendOrEditMessageAsync(
             cache,
            update,
            photo: new InputFileStream(stream),
            caption: text,
            parseMode: ParseMode.Html
        );
    }
}