using Microsoft.Extensions.DependencyInjection;
using SummyAITelegramBot.Core.Bot.Abstractions;
using SummyAITelegramBot.Core.Bot.Attributes;
using System.Reflection;
using Telegram.Bot.Types;

namespace SummyAITelegramBot.Core.Bot.Factories;

public class TelegramUpdateFactory : ITelegramUpdateFactory
{
    private readonly Dictionary<string, ITelegramUpdateHandler> _handlersByPrefix;

    public TelegramUpdateFactory(IServiceScopeFactory scopeFactory)
    {
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

    public async Task DispatchAsync(Message query, string prefix)
    {
        if (string.IsNullOrEmpty(prefix)) return;

        if (_handlersByPrefix.TryGetValue(prefix, out var handler))
        {
            await handler.HandleAsync(query);
        }
    }
}