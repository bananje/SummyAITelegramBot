using SummyAITelegramBot.Core.Abstractions;
using SummyAITelegramBot.Core.Bot.Abstractions;
using ChannelEn = SummyAITelegramBot.Core.Domain.Models.Channel;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Microsoft.EntityFrameworkCore;
using SummyAITelegramBot.Core.Bot.Features.Channel.Abstractions;
using UserEn = SummyAITelegramBot.Core.Domain.Models.User;

namespace SummyAITelegramBot.Core.Bot.Features.Channel.Handlers;

public class AddChannelHandler(
    IUnitOfWork unitOfWork,
    ITelegramBotClient bot,
    ITelegramChannelAdapter channelAdapter) : IStepOnChainHandler
{
    public IStepOnChainHandler? Next { get; set; }

    public async Task HandleAsync(Update update)
    {
        var channelLink = update.Message.Text;
        var userId = update.Message.From.Id;
        var channelRepository = unitOfWork.Repository<long, ChannelEn>();
        var userRepository = unitOfWork.Repository<long, UserEn>();
       
        if (await channelRepository.GetIQueryable().AnyAsync(u => u.Link == channelLink))
        {
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                InlineKeyboardButton.WithCallbackData("Добавить канал", "/add"),
                InlineKeyboardButton.WithCallbackData("Завершить добавление", "/settings")
            });

            await bot.SendMessage(
                update.Message.Chat.Id,
                "Этот канал уже добавлен в вашу коллекцию:",
                replyMarkup: keyboard);
        }

        var channelInfo = await channelAdapter.ResolveChannelAsync(channelLink);
        var channel = new ChannelEn
        {
            HasStopFactor = channelInfo!.flags.HasFlag(TL.Channel.Flags.fake)
                || channelInfo.flags.HasFlag(TL.Channel.Flags.scam),
            Link = channelLink,
            Id = channelInfo.id,
        };

        await channelRepository.AddAsync(channel);

        var user = await userRepository.GetByIdAsync(userId)
            ?? throw new Exception($"Ошибка при настройке пользователя {userId}.");
        user.AddChannel(channel);

        await unitOfWork.CommitAsync();

        if (Next != null)
            await Next.ShowStepAsync(update);
    }

    public async Task ShowStepAsync(Update update)
    {
        var userId = update.Message.From!.Id;
        var userRepository = unitOfWork.Repository<long, Domain.Models.User>();

        var user = await userRepository.GetByIdAsync(userId)
            ?? throw new Exception($"Ошибка при настройке пользователя {userId}.");

        if (!user.HasSubscriptionPremium && user.Channels.Count > 5) 
        {
            await bot.SendMessage(
                update.Message.Chat.Id,
                "Оплатите премиум"
            );
        }

        await bot.SendMessage(
            update.Message.Chat.Id,
            "Отправьте ссылку на канал:",
            replyMarkup: new ForceReplyMarkup { Selective = true }
        );
    }
}