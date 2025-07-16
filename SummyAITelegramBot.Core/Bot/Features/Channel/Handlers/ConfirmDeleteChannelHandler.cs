using SummyAITelegramBot.Core.Abstractions;
using SummyAITelegramBot.Core.Bot.Abstractions;
using SummyAITelegramBot.Core.Bot.Attributes;
using Telegram.Bot.Types;
using Telegram.Bot;
using Microsoft.EntityFrameworkCore;
using SummyAITelegramBot.Core.Bot.Extensions;
using SummyAITelegramBot.Core.Bot.Utils;

namespace SummyAITelegramBot.Core.Bot.Features.Channel.Handlers;

[TelegramUpdateHandler("/confirmdelete")]
public class ConfirmDeleteChannelHandler(
    ITelegramBotClient bot,
    ITelegramUpdateFactory telegramUpdateFactory,
    IUnitOfWork unitOfWork) : ITelegramUpdateHandler
{
    private readonly IRepository<long, Domain.Models.User> _userRepository = unitOfWork.Repository<long, Domain.Models.User>();
    private readonly IRepository<long, Domain.Models.Channel> _channelRepository = unitOfWork.Repository<long, Domain.Models.Channel>();

    public async Task HandleAsync(Update update)
    {
        var callbackData = update.CallbackQuery?.Data
            ?? throw new Exception("Нет данных из callback");

        var parts = callbackData.Split(':');
        if (parts.Length != 2 || !long.TryParse(parts[1], out var channelId))
            throw new Exception("Неверный формат команды удаления");

        var (userId, chatId) = TelegramHelper.GetUserAndChatId(update);

        var user = await _userRepository.GetIQueryable()
            .Include(u => u.Channels)
            .FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new Exception("Пользователь не найден");

        var channel = user.Channels.FirstOrDefault(c => c.Id == channelId);
        if (channel == null)
        {
            await bot.ReactivelySendAsync(
                chatId,
                "⚠️ Канал не найден или уже удалён.",
                userMessage: update.CallbackQuery.Message
            );
            return;
        }

        // Удаляем канал
        await _channelRepository.RemoveAsync(channel.Id);
        await unitOfWork.CommitAsync();

        await telegramUpdateFactory.DispatchAsync(update, "/mychannels");
    }
}