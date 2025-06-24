using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using SummyAITelegramBot.Core.Abstractions;
using SummyAITelegramBot.Core.Bot.Abstractions;
using SummyAITelegramBot.Core.Bot.Attributes;
using SummyAITelegramBot.Core.Bot.Extensions;
using System.IO;
using System.Reflection;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace SummyAITelegramBot.Core.Bot.Factories;

public class TelegramUpdateFactory : ITelegramUpdateFactory
{
    private readonly Dictionary<string, ITelegramUpdateHandler> _handlersByPrefix;
    private readonly ITelegramBotClient _bot;
    private readonly IMemoryCache _cache;
    private readonly IStaticImageService _imageService;

    public TelegramUpdateFactory(
        IServiceScopeFactory scopeFactory,
        ITelegramBotClient bot,
        IStaticImageService imageService,
        IMemoryCache cache)
    {
        _imageService = imageService;
        _bot = bot;
        _cache = cache;
        _handlersByPrefix = new();

        var handlerTypes = AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(x => x.GetTypes())
            .Where(t => typeof(ITelegramUpdateHandler)
            .IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

        foreach (var type in handlerTypes)
        {
            var attr = type.GetCustomAttribute<TelegramUpdateHandlerAttribute>();
            if (attr != null)
            {
                var scope = scopeFactory.CreateScope();
                var handler = (ITelegramUpdateHandler)scope.ServiceProvider.GetRequiredService(type);
                _handlersByPrefix[attr.Prefix] = handler;
            }
        }
    }

    public async Task DispatchAsync(Update query, string prefix)
    {
        if (string.IsNullOrEmpty(prefix)) return;

        if (_handlersByPrefix.TryGetValue(prefix, out var handler))
        {
            await handler.HandleAsync(query);
        }
        else
        {
            var text = $"""
                <b>Неизвестная комманда или ссылка</b>

                *Проверьте ссылку или команду и отправьте снова
                """;
            await using var failStream = _imageService.GetImageStream("add_channel.jpg");

            //await _bot.SendOrEditMessageAsync(
            //    _cache,
            //    query,
            //    photo: new InputFileStream(failStream),
            //    caption: text,
            //    parseMode: ParseMode.Html);
        }
    }
}