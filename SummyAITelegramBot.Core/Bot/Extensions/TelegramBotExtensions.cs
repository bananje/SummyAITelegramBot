using Microsoft.Extensions.Caching.Memory;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;

namespace SummyAITelegramBot.Core.Bot.Extensions;

public static class TelegramBotClientExtensions
{
    private static int _prevMsgId = 0;

    public static async Task SendOrEditMessageAsync(
    this ITelegramBotClient botClient,
    IMemoryCache cache,
    Update update,
    string? botText = null,
    InputFile? photo = null,
    string? caption = null,
    ParseMode? parseMode = null,
    InlineKeyboardMarkup? replyMarkup = null,
    bool deleteUserMessage = true,
    TimeSpan? cacheTtl = null)
    {
        ArgumentNullException.ThrowIfNull(botClient);
        ArgumentNullException.ThrowIfNull(update);
        ArgumentNullException.ThrowIfNull(cache);

        var userMessage = update.Message ?? update.CallbackQuery?.Message;

        if (userMessage == null)
            throw new InvalidOperationException("Update must contain a user message.");

        if (userMessage.From == null)
            throw new InvalidOperationException("User message must have sender info.");

        var chatId = userMessage.Chat.Id;
        var userId = userMessage.From.Id;
        string cacheKey = $"EditMessage:{chatId}:{userId}";
        cacheTtl ??= TimeSpan.FromMinutes(10);

        // 1. Удаляем сообщение пользователя
        if (deleteUserMessage && update.Message is not null)
        {
            try
            {
                await botClient.DeleteMessage(chatId, userMessage.MessageId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Bot] Не удалось удалить сообщение пользователя: {ex.Message}");
            }
        }

        // 2. Попытка редактировать предыдущее сообщение бота
        if (cache.TryGetValue<int>(cacheKey, out var oldBotMessageId))
        {
            try
            {
                Message sendedMessage = default;
                if (photo is not null)
                {
                    var media = new InputMediaPhoto(photo)
                    {
                        Caption = caption,
                        ParseMode = parseMode.Value
                    };

                    sendedMessage = await botClient.EditMessageMedia(
                        chatId: chatId,
                        messageId: oldBotMessageId,
                        media: media,
                        replyMarkup: replyMarkup
                    );
                }
                else if (!string.IsNullOrWhiteSpace(botText))
                {
                    sendedMessage = await botClient.EditMessageText(
                        chatId: chatId,
                        messageId: oldBotMessageId,
                        text: botText,
                        parseMode: parseMode ?? ParseMode.None,
                        replyMarkup: replyMarkup
                    );
                }
                else
                {
                    throw new ArgumentException("Either botText or photo must be provided.");
                }

                cache.Remove(cacheKey);
                cache.Set(cacheKey, sendedMessage.Id, cacheTtl.Value);

                // успешно отредактировано — выходим
                return;
            }
            catch (ApiRequestException ex) when (ex.Message.Contains("message is not modified"))
            {
                if (userMessage.Text != "/start")
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Bot] Не удалось отредактировать сообщение: {ex.Message}");

                // попытка удалить старое сообщение
                try
                {
                    await botClient.DeleteMessage(chatId, oldBotMessageId);
                }
                catch (Exception deleteEx)
                {
                    Console.WriteLine($"[Bot] Не удалось удалить старое сообщение: {deleteEx.Message}");
                }

                cache.Remove(cacheKey);
            }
        }
        else
        {
            if (_prevMsgId != 0) { await botClient.DeleteMessage(chatId, _prevMsgId); }

            // 3. Отправка нового сообщения
            Message sentMessage;
            if (photo is not null)
            {
                sentMessage = await botClient.SendPhoto(
                    chatId: chatId,
                    photo: photo,
                    caption: caption,
                    parseMode: parseMode ?? ParseMode.None,
                    replyMarkup: replyMarkup
                );
            }
            else if (!string.IsNullOrWhiteSpace(botText))
            {
                sentMessage = await botClient.SendMessage(
                    chatId: chatId,
                    text: botText,
                    parseMode: parseMode ?? ParseMode.None,
                    replyMarkup: replyMarkup
                );
            }
            else
            {
                throw new ArgumentException("Either botText or photo must be provided.");
            }

            cache.Set(cacheKey, sentMessage.MessageId, cacheTtl.Value);
            _prevMsgId = sentMessage.MessageId;
        }
    }
}