using SummyAITelegramBot.Core.Abstractions;
using SummyAITelegramBot.Core.Bot.Abstractions;
using SummyAITelegramBot.Core.Bot.Attributes;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot;
using SummyAITelegramBot.Core.Domain.Models;
using Telegram.Bot.Types;
using Microsoft.EntityFrameworkCore;
using SummyAITelegramBot.Core.Bot.Extensions;
using SummyAITelegramBot.Core.Bot.Utils;

namespace SummyAITelegramBot.Core.Bot.Handlers;

[TelegramUpdateHandler("/deletechannel")]
public class DeleteChannelConfirmationHandler(
    ITelegramBotClient bot,
    IUnitOfWork unitOfWork) : ITelegramUpdateHandler
{
    private readonly IRepository<long, Domain.Models.User> _userRepository = unitOfWork.Repository<long, Domain.Models.User>();
    private readonly IRepository<long, Channel> _channelRepository = unitOfWork.Repository<long, Channel>();

    public async Task HandleAsync(Update update)
    {
        var callbackData = update.CallbackQuery?.Data
            ?? throw new Exception("Нет данных из callback-кнопки");

        var parts = callbackData.Split(':');
        if (parts.Length != 2 || !long.TryParse(parts[1], out var channelId))
            throw new Exception("Неверный формат команды удаления канала");

        var (userId, chatId) = TelegramHelper.GetUserAndChatId(update);

        var user = await _userRepository.GetIQueryable()
            .Include(u => u.Channels)
            .FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new Exception("Пользователь не найден");

        var channel = user.Channels.FirstOrDefault(c => c.Id == channelId)
            ?? throw new Exception("Канал не найден у пользователя");

        var text = $"""
            ⚠️ <b>Удаление канала</b>

            Вы уверены, что хотите удалить канал:
            📢 <a href="{channel.Link}">{channel.Title}</a>

            <i>После удаления сводки по нему больше не будут приходить</i>
            """;

        var buttons = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("✅ Удалить", $"/confirmdelete:{channel.Id}"),
                InlineKeyboardButton.WithCallbackData("❌ Отмена", "/mychannels")
            }
        });

        await bot.ReactivelySendAsync(
            chatId,
            text,
            replyMarkup: buttons,
            userMessage: update.CallbackQuery.Message
        );
    }
}