using Microsoft.Extensions.Caching.Memory;
using Telegram.Bot.Exceptions;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Serilog;

namespace SummyAITelegramBot.Core.Bot.Extensions;

public static class TelegramBotClientExtensions
{
    private static readonly MemoryCache _cache = new(new MemoryCacheOptions());
    private static readonly TimeSpan _cacheLifetime = TimeSpan.FromMinutes(30);

    public static async Task ReactivelySendAsync(
        this ITelegramBotClient bot,
        long chatId,
        string text,
        Message? userMessage = null,
        InlineKeyboardMarkup? replyMarkup = null,
        CancellationToken cancellationToken = default)
    {
        // Удаляем сообщение пользователя
        if (userMessage is { From.IsBot: false })
        {
            try
            {
                await bot.DeleteMessage(chatId, userMessage.MessageId, cancellationToken);
            }
            catch { /* игнор */ }
        }

        if (_cache.TryGetValue(chatId, out CachedBotMessage? previous))
        {
            if (previous?.Type == MessageType.Text)
            {
                try
                {
                    await bot.EditMessageText(
                        chatId,
                        previous.MessageId,
                        text,
                        replyMarkup: replyMarkup,
                        parseMode: ParseMode.Html,
                        cancellationToken: cancellationToken);

                    return; // успешно отредактировали — выходим
                }
                catch (ApiRequestException ex) when (ex.ErrorCode is 400 or 403)
                {
                    // не получилось — продолжим удалять и пересылать
                }
            }

            try
            {
                await bot.DeleteMessage(chatId, previous.MessageId, cancellationToken);
            }
            catch { /* игнор */ }
        }

        var sent = await bot.SendMessage(
            chatId,
            text,
            replyMarkup: replyMarkup,
            parseMode: ParseMode.Html,
            cancellationToken: cancellationToken);

        _cache.Set(chatId, new CachedBotMessage
        {
            MessageId = sent.MessageId,
            Type = MessageType.Text
        }, _cacheLifetime);
    }

    public static async Task ReactivelySendPhotoAsync(
        this ITelegramBotClient bot,
        long chatId,
        InputFileStream photo,
        string? caption = null,
        Message? userMessage = null,
        InlineKeyboardMarkup? replyMarkup = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Удаляем сообщение пользователя
            if (userMessage is not null && userMessage is { From.IsBot: false })
            {
                try
                {
                    await bot.DeleteMessage(chatId, userMessage.MessageId, cancellationToken);
                }
                catch { /* игнор */ }
            }

            if (_cache.TryGetValue(chatId, out CachedBotMessage? previous))
            {
                if (previous?.Type == MessageType.Photo)
                {
                    try
                    {
                        await bot.EditMessageCaption(
                            chatId,
                            previous.MessageId,
                            caption,
                            replyMarkup: replyMarkup,
                            parseMode: ParseMode.Html,
                            cancellationToken: cancellationToken);

                        return;
                    }
                    catch (ApiRequestException ex) when (ex.ErrorCode is 400 or 403)
                    {
                        // не удалось редактировать
                    }
                }

                try
                {
                    await bot.DeleteMessage(chatId, previous.MessageId, cancellationToken);
                }
                catch { /* игнор */ }
            }

            var sent = await bot.SendPhoto(
                chatId,
                photo,
                caption: caption,
                parseMode: ParseMode.Html,
                replyMarkup: replyMarkup,
                cancellationToken: cancellationToken);

            _cache.Set(chatId, new CachedBotMessage
            {
                MessageId = sent.MessageId,
                Type = MessageType.Photo
            }, _cacheLifetime);
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex.Message);
        }   
    }

    private class CachedBotMessage
    {
        public int MessageId { get; set; }
        public MessageType Type { get; set; }
    }
}