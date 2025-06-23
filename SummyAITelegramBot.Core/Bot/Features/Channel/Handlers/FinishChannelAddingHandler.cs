using FluentResults;
using Microsoft.Extensions.Caching.Memory;
using SummyAITelegramBot.Core.Abstractions;
using SummyAITelegramBot.Core.Bot.Abstractions;
using SummyAITelegramBot.Core.Bot.Extensions;
using SummyAITelegramBot.Core.Domain.Models;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace SummyAITelegramBot.Core.Bot.Features.Channel.Handlers;

public class FinishChannelAddingHandler(
    ITelegramBotClient bot,
    IUnitOfWork unitOfWork,
    IStaticImageService imageService,
    IMemoryCache cache) : IStepOnChainHandler<UserSettings>
{
    public IStepOnChainHandler<UserSettings>? Next { get; set; }

    public Task<Result> HandleAsync(Update update, UserSettings? entity = null)
    {
        return Task.FromResult(Result.Ok());
    }

    public async Task ShowStepAsync(Update update)
    {
        var text = $"""
                <b>Канал успешно добавлен в вашу библиотеку</b>

                Для добавления других каналов, нажмите (Канал📣)

                *Сводки будут прилетать в этот чат, согласно вашим настройкам 📢
                """;

        await using var stream = imageService.GetImageStream("add_channel.jpg");

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            InlineKeyboardButton.WithCallbackData("Канал📣", "/add"),
        });

        await bot.SendOrEditMessageAsync(
            cache, update, photo: stream, replyMarkup: keyboard, caption: text,
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Html);
    }
}