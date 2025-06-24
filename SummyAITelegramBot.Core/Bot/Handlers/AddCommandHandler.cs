using Microsoft.Extensions.Caching.Memory;
using SummyAITelegramBot.Core.Abstractions;
using SummyAITelegramBot.Core.Bot.Abstractions;
using SummyAITelegramBot.Core.Bot.Attributes;
using SummyAITelegramBot.Core.Bot.Extensions;
using SummyAITelegramBot.Core.Bot.Utils;
using SummyAITelegramBot.Core.Domain.Models;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace SummyAITelegramBot.Core.Bot.Handlers;

[TelegramUpdateHandler("/add")]
public class AddCommandHandler(
    IUnitOfWork unitOfWork,
    ITelegramBotClient bot,
    IMemoryCache cache,
    IStaticImageService imageService) : ITelegramUpdateHandler
{
    public async Task HandleAsync(Update update)
    {
        var chatId = update.Message.Chat.Id;
        var userRepository = unitOfWork.Repository<long, Domain.Models.User>();

        var user = await userRepository.GetByIdAsync(chatId)
            ?? throw new Exception($"Ошибка при настройке пользователя {chatId}.");

        if (!user.HasSubscriptionPremium && user.Channels.Count > 5)
        {
            //await bot.SendOrEditMessageAsync(
            //    cache,
            //    update,
            //    "Оплатите премиум"
            //);
        }

        var text = $"""
                1️⃣ <b>Добавьте Ваши каналы</b>

                Просто отправьте ссылку на канал
                (Пример: https://t.me/UseSummyAI)

                <b> *В базовом тарифе можно добавить до 5 каналов 📢</b>
                """;

        await using var stream = imageService.GetImageStream("add_channel.jpg");

        await bot.ReactivelySendPhotoAsync(
            chatId,
            photo: new InputFileStream(stream),
            userMessage: update.Message,
            caption: text 
        );
    }
}
