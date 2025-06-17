using Microsoft.EntityFrameworkCore;
using SummyAITelegramBot.Core.Abstractions;
using SummyAITelegramBot.Core.Bot.Abstractions;
using SummyAITelegramBot.Core.Bot.Attributes;
using SummyAITelegramBot.Core.Bot.Features.Channel.Abstractions;
using SummyAITelegramBot.Core.Domain.Models;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace SummyAITelegramBot.Core.Bot.ReplyHandlers;

[ReplyHandlerAttribute("Отправьте ссылку на канал:")]
public class ChannelLinkReplyHandler(
    IUnitOfWork unitOfWork,
    ITelegramBotClient botClient,
    ITelegramChannelAdapter telegramChannelService) : IReplyHandler
{
    public async Task HandleAsync(Message replyMessage)
    {
        var channelLink = replyMessage.Text;
        var chatId = replyMessage.Chat.Id;
        var userId = replyMessage.From.Id;

        var channelRepository = unitOfWork.Repository<long, Channel>();
        var userRepository = unitOfWork.Repository<long, Domain.Models.User>();

        var channelExist = await channelRepository.GetIQueryable()
            .AnyAsync(u => u.Link == replyMessage.Text);

        if (channelExist)
        {
            await botClient.SendMessage(chatId, "Вы уже добавили такой канал ✅");

            // TODO: перенаправление на изначальную страницу, где можно выбрать добавление канала
        }

        var user = await userRepository.GetByIdAsync(userId)
            ?? throw new Exception($"Ошибка при настройке пользователя {userId}.");

        var channelInfo = await telegramChannelService.ResolveChannelAsync(channelLink);

        var channel = new Channel
        {
            HasStopFactor = channelInfo!.flags.HasFlag(TL.Channel.Flags.fake)
                || channelInfo.flags.HasFlag(TL.Channel.Flags.scam),
            Link = channelLink,
        };

        await channelRepository.CreateOrUpdateAsync(channel);

        user.AddChannel(channel);

        await unitOfWork.CommitAsync();

        // TODO: Добавить предупреждение об иноагентах, скаме и мошенничестве
        await botClient.SendMessage(chatId, "Канал успешно добавлен ✅");
    }
}
