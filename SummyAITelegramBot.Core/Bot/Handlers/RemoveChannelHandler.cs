using Microsoft.EntityFrameworkCore;
using SummyAITelegramBot.Core.Abstractions;
using SummyAITelegramBot.Core.Bot.Abstractions;
using SummyAITelegramBot.Core.Bot.Attributes;
using SummyAITelegramBot.Core.Bot.Extensions;
using SummyAITelegramBot.Core.Bot.Features.Channel.Abstractions;
using SummyAITelegramBot.Core.Domain.Models;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace SummyAITelegramBot.Core.Bot.Handlers;

[TelegramUpdateHandler("/remove")]
public class RemoveChannelHandler(
    ITelegramBotClient bot,
    IStaticImageService staticImageService,
    ITelegramUpdateFactory telegramUpdateFactory,
    ITelegramChannelAdapter telegramChannelAdapter,
    IUnitOfWork unitOfWork) : ITelegramUpdateHandler
{
    private readonly IRepository<long, Domain.Models.User> _userRepository = unitOfWork.Repository<long, Domain.Models.User>();
    private readonly IRepository<long, Channel> _channelRepository = unitOfWork.Repository<long, Channel>();

    public async Task HandleAsync(Update update)
    {
        var (userId, chatId) = GetUserAndChatId(update);
        var messageText = update.Message?.Text ?? update.CallbackQuery?.Data ?? "";

        var keyboard = new InlineKeyboardMarkup(new[]
        {
             new[] { InlineKeyboardButton.WithCallbackData("🚀 Мои каналы", "/showchannels") },
        });

        var parts = messageText.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            await using var stream = staticImageService.GetImageStream("add_channel.jpg");

            await bot.ReactivelySendPhotoAsync(
                chatId, 
                caption:"⚠️ Укажите ссылку на канал для удаления. \n Пример: /remove t.me/example",
                replyMarkup: keyboard,
                photo: new InputFileStream(stream));

            return;
        }

        var channelLink = parts[1].Trim();

        var user = await _userRepository.GetIQueryable()
            .Where(u => u.Id == userId)
            .Include(u => u.Channels)
            .FirstOrDefaultAsync()
            ?? throw new Exception($"Пользователь {userId} не найден.");

        try
        {
            var channelInfo = await telegramChannelAdapter.ResolveChannelAsync(channelLink);

            await _channelRepository.RemoveAsync(channelInfo.id);
            await unitOfWork.CommitAsync();

            await telegramUpdateFactory.DispatchAsync(update, "/showchannels");

            return;
        }
        catch (Exception ex)
        {
            var text = $"""
                ⚠️ <b>Такого канала нет в вашем списке</b>
                """;

            await using var stream = staticImageService.GetImageStream("add_channel.jpg");

            await bot.ReactivelySendPhotoAsync(chatId, 
                caption: text, 
                photo: new InputFileStream(stream), 
                replyMarkup: keyboard);

            return;
        }
    }

    private (long userId, long chatId) GetUserAndChatId(Update update)
    {
        if (update.Message != null)
        {
            return (update.Message.From.Id, update.Message.Chat.Id);
        }

        if (update.CallbackQuery != null)
        {
            return (update.CallbackQuery.From.Id, update.CallbackQuery.Message.Chat.Id);
        }

        throw new Exception("Неизвестный формат обновления");
    }
}