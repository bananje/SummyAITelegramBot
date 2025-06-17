using Microsoft.EntityFrameworkCore;
using SummyAITelegramBot.Core.Abstractions;
using SummyAITelegramBot.Core.Bot.Abstractions;
using SummyAITelegramBot.Core.Bot.Attributes;
using SummyAITelegramBot.Core.Bot.Features.Channel.Abstractions;
using SummyAITelegramBot.Core.Domain.Models;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace SummyAITelegramBot.Core.Bot.Features.Channel.Commands;

/// <summary>
/// Обработчик команды добавления канала
/// </summary>
[CommandHandler("add")]
public class AddChannelCommandHandler(
    ITelegramBotClient botClient,
    ITelegramChannelAdapter telegramChannelService,
    IUnitOfWork unitOfWork
    ) : ICommandHandler
{
    public async Task HandleAsync(Message message)
    {
        var chatId = message.Chat.Id;
        var userId = message.From!.Id;
        var channelLink = message.Text;

        //var channelRepository = unitOfWork.Repository<long, Channel>();
        var userRepository = unitOfWork.Repository<long, Domain.Models.User>();

        var user = await userRepository.GetByIdAsync(userId)
            ?? throw new Exception($"Ошибка при настройке пользователя {userId}.");

        await botClient.SendMessage(
            chatId,
            "Отправьте ссылку на канал:",
            replyMarkup: new ForceReplyMarkup { Selective = true }
        );
    }
}