using SummyAITelegramBot.Core.Bot.Abstractions;
using SummyAITelegramBot.Core.Domain.Models;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types;
using Telegram.Bot;
using SummyAITelegramBot.Core.Abstractions;
using Telegram.Bot.Types.Enums;

namespace SummyAITelegramBot.Core.Bot.Features.Settings.Handlers;

public class MediaStepHandler(IStaticImageService imageService) : IChainOfStepsHandler<UserSettings>
{
    public IChainOfStepsHandler<UserSettings>? Next { get; set; }

    public async Task HandleAsync(ITelegramBotClient bot, CallbackQuery query, UserSettings entity)
    {
        if (query.Data == "settings:media:yes")
            entity.MediaEnabled = true;
        else if (query.Data == "settings:media:no")
            entity.MediaEnabled = false;

        if (Next != null)
            await Next.ShowStepAsync(bot, query.Message!.Chat.Id);
    }

    public async Task ShowStepAsync(ITelegramBotClient bot, long chatId)
    {
        var caption =
            "<b>Пример сводки с медиа</b>\n\n" +
            "⚡️Футбольный клуб «Краснодар» впервые стал Чемпионом России по футболу.\n\n" +
            " Матч проходил в стадионе парка 'Краснодар'\n\n" +
            "<a href='https://example.com'>Ссылка</a>";

        var inlineKeyboard = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("🔈 Включить медиа контент", "settings:media:yes") },
            new[] { InlineKeyboardButton.WithCallbackData("✅ Оставить так и продолжить", "settings:media:no") }
        });

        await using var stream = imageService.GetImageStream("mediaexample.jpg");
        await bot.SendPhoto(
            chatId: chatId,
            photo: new InputFileStream(stream),
            caption: caption,
            parseMode: ParseMode.Html,
            replyMarkup: inlineKeyboard
        );
    }
}
