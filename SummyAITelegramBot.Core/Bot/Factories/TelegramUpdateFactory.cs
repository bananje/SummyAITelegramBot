﻿using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using SummyAITelegramBot.Core.Abstractions;
using SummyAITelegramBot.Core.Bot.Abstractions;
using SummyAITelegramBot.Core.Bot.Attributes;
using SummyAITelegramBot.Core.Bot.Extensions;
using SummyAITelegramBot.Core.Bot.Utils;
using System.Reflection;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

public class TelegramUpdateFactory : ITelegramUpdateFactory
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ITelegramBotClient _bot;
    private readonly IUserCommandCache _commandCache;
    private readonly IMemoryCache _cache;
    private readonly IStaticImageService _imageService;

    public TelegramUpdateFactory(
        IServiceScopeFactory scopeFactory,
        IUserCommandCache commandCache,
        ITelegramBotClient bot,
        IMemoryCache cache,
        IStaticImageService imageService)
    {
        _commandCache = commandCache;
        _cache = cache;
        _scopeFactory = scopeFactory;
        _bot = bot;
        _imageService = imageService;
    }

    public async Task DispatchAsync(Update query, string prefix)
    {
        var (chatId, userId) = TelegramHelper.GetUserAndChatId(query);

        if (string.IsNullOrEmpty(prefix)) return;

        using var scope = _scopeFactory.CreateScope();

        // Найти тип обработчика с нужным атрибутом
        var handlerType = GetHandler(prefix);

        if (handlerType != null)
        {
            try
            {
                var handler = (ITelegramUpdateHandler)scope.ServiceProvider.GetRequiredService(handlerType);
                await handler.HandleAsync(query);

                _commandCache.SetLastCommand(userId, prefix);
            }
            catch (Exception ex)
            {
                var startHandler = GetHandler("/start");

                var handler = (ITelegramUpdateHandler)scope.ServiceProvider.GetRequiredService(startHandler);
                await handler.HandleAsync(query);

                Log.Error(ex, ex.Message);
            }
        }
        else
        {

            var message = query.Message is null 
                ? query.CallbackQuery.Message 
                : query.Message;

            // Нет обработчика для данного префикса — логика ошибки/уведомления
            var text = $"""
            <b>Неизвестная команда или ссылка</b>

            *Проверьте ссылку или команду и отправьте снова
            """;
            var failStream = _imageService.GetImageStream("add_channel.jpg");

            var keyboard = new InlineKeyboardMarkup(new[]
            {
                 new[] { InlineKeyboardButton.WithCallbackData("🦉 Личный кабинет", "/account") },
            });

            await _bot.ReactivelySendPhotoAsync(
                message.Chat.Id,
                photo: failStream,
                userMessage: query.Message,
                replyMarkup: keyboard,
                caption: text
            );
        }
    }

    private Type? GetHandler(string prefix)
    {
        var handlerType = AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(x => x.GetTypes())
            .Where(t => typeof(ITelegramUpdateHandler).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
            .FirstOrDefault(t =>
            {
                var attr = t.GetCustomAttribute<TelegramUpdateHandlerAttribute>();
                return attr != null && attr.Prefix == prefix;
            });

        return handlerType;
    }
}