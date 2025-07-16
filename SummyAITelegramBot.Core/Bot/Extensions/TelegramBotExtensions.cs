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
            try
            {
                switch (previous?.Type)
                {
                    case MessageType.Text:
                        await bot.EditMessageText(
                            chatId,
                            previous.MessageId,
                            text,
                            replyMarkup: replyMarkup,
                            parseMode: ParseMode.Html,
                            cancellationToken: cancellationToken);
                        return;

                    case MessageType.Photo:
                        await bot.EditMessageCaption(
                            chatId,
                            previous.MessageId,
                            caption: text, // здесь text = новая подпись
                            replyMarkup: replyMarkup,
                            parseMode: ParseMode.Html,
                            cancellationToken: cancellationToken);
                        return;

                    // если хотите обрабатывать другие типы, можно добавить сюда

                    default:
                        break; // пойдём на удаление + пересылку
                }
            }
            catch (ApiRequestException ex) when (ex.ErrorCode is 400 or 403)
            {
                // не получилось — удалим и пересоздадим
            }           
        }

        try
        {
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
        catch (Exception ex)
        {
            Log.Error(ex, ex.Message);
        }
    }

    public static async Task ReactivelySendPhotoAsync(
    this ITelegramBotClient bot,
    long chatId,
    Stream photo,
    string? caption = null,
    Message? userMessage = null,
    InlineKeyboardMarkup? replyMarkup = null,
    CancellationToken cancellationToken = default)
    {
        try
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

            // Копируем оригинальный поток в память (на случай повторного использования)
            using var memoryStream = new MemoryStream();
            await photo.CopyToAsync(memoryStream, cancellationToken);
            memoryStream.Position = 0;

            if (_cache.TryGetValue(chatId, out CachedBotMessage? previous))
            {
                if (previous?.Type == MessageType.Photo)
                {
                    try
                    {
                        var editStream = new MemoryStream(memoryStream.ToArray());

                        await bot.EditMessageMedia(
                            chatId: chatId,
                            messageId: previous.MessageId,
                            media: new InputMediaPhoto(new InputFileStream(editStream))
                            {
                                Caption = caption,
                                ParseMode = ParseMode.Html
                            },
                            replyMarkup: replyMarkup,
                            cancellationToken: cancellationToken);

                        return;
                    }
                    catch (ApiRequestException ex) when (ex.ErrorCode is 400 or 403)
                    {
                        // не удалось редактировать, удалим
                    }
                }

                try
                {
                    await bot.DeleteMessage(chatId, previous.MessageId, cancellationToken);
                }
                catch { /* игнор */ }
            }

            // Отправка нового фото
            memoryStream.Position = 0;
            var sendStream = new MemoryStream(memoryStream.ToArray());

            var sent = await bot.SendPhoto(
                chatId,
                new InputFileStream(sendStream),
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
            Log.Logger.Error(ex, ex.Message);
        }
    }


    private class CachedBotMessage
    {
        public int MessageId { get; set; }
        public MessageType Type { get; set; }
    }
}