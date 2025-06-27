using Microsoft.Extensions.DependencyInjection;
using SummyAITelegramBot.Core.Abstractions;
using SummyAITelegramBot.Core.Bot.Abstractions;
using SummyAITelegramBot.Core.Bot.Attributes;
using SummyAITelegramBot.Core.Bot.Extensions;
using System.Reflection;
using Telegram.Bot;
using Telegram.Bot.Types;

public class TelegramUpdateFactory : ITelegramUpdateFactory
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ITelegramBotClient _bot;
    private readonly IStaticImageService _imageService;

    public TelegramUpdateFactory(
        IServiceScopeFactory scopeFactory,
        ITelegramBotClient bot,
        IStaticImageService imageService)
    {
        _scopeFactory = scopeFactory;
        _bot = bot;
        _imageService = imageService;
    }

    public async Task DispatchAsync(Update query, string prefix)
    {
        if (string.IsNullOrEmpty(prefix)) return;

        using var scope = _scopeFactory.CreateScope();

        // Найти тип обработчика с нужным атрибутом
        var handlerType = AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(x => x.GetTypes())
            .Where(t => typeof(ITelegramUpdateHandler).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
            .FirstOrDefault(t =>
            {
                var attr = t.GetCustomAttribute<TelegramUpdateHandlerAttribute>();
                return attr != null && attr.Prefix == prefix;
            });

        if (handlerType != null)
        {
            var handler = (ITelegramUpdateHandler)scope.ServiceProvider.GetRequiredService(handlerType);
            await handler.HandleAsync(query);
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
            await using var failStream = _imageService.GetImageStream("add_channel.jpg");

            await _bot.ReactivelySendPhotoAsync(
                message.Chat.Id,
                photo: new InputFileStream(failStream),
                userMessage: query.Message,
                caption: text
            );
        }
    }
}