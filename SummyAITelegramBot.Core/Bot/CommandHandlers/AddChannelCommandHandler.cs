using Microsoft.EntityFrameworkCore;
using SummyAITelegramBot.Core.Abstractions;
using SummyAITelegramBot.Core.Bot.Abstractions;
using SummyAITelegramBot.Core.Bot.Attributes;
using SummyAITelegramBot.Core.Domain.Models;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace SummyAITelegramBot.Core.Bot.CommandHandlers;

/// <summary>
/// Обработчик команды добавления канала
/// </summary>
[CommandHandler("add")]
public class AddChannelCommandHandler(
    IRepository<Guid, Channel> channelRepository,
    IRepository<long, Domain.Models.User> userRepository,
    ITelegramBotClient botClient
    ) : ICommandHandler
{
    public async Task HandleAsync(Message message)
    {
        var chatId = message.Chat.Id;
        var userId = message.From!.Id;
        var channelLink = message.Text;

        var user = await userRepository.GetByIdAsync(userId)
            ?? throw new Exception($"Ошибка при настройке пользователя {userId}.");

        var channelExist = await channelRepository.GetIQueryable().AnyAsync(u => u.Link == channelLink);

        if (channelExist)
        {
            await botClient.SendMessage(chatId, "Вы уже добавили такой канал ✅");

            // TODO: перенаправление на изначальную страницу, где можно выбрать добавление канала
        }

        var channel = new Channel
        {
            
        };

        //await channelRepository.AddAsync();

        return;
    }
}