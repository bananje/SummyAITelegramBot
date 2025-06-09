using Microsoft.Extensions.DependencyInjection;
using SummyAITelegramBot.Core.Bot.Abstractions;
using SummyAITelegramBot.Core.Bot.Attributes;
using System.Reflection;
using Telegram.Bot.Types;

namespace SummyAITelegramBot.Core.Factories;

public class CallbackFactory : ICallbackFactory
{
    private readonly Dictionary<string, ICallbackHandler> _handlersByPrefix;

    public CallbackFactory(IServiceScopeFactory scopeFactory)
    {
        _handlersByPrefix = new();

        var handlerTypes = AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(x => x.GetTypes())
            .Where(t => typeof(ICallbackHandler).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

        foreach (var type in handlerTypes)
        {
            var attr = type.GetCustomAttribute<CallbackHandlerAttribute>();
            if (attr != null)
            {
                var scope = scopeFactory.CreateScope();
                var handler = (ICallbackHandler)scope.ServiceProvider.GetRequiredService(type);
                _handlersByPrefix[attr.Prefix] = handler;
            }
        }
    }


    public async Task DispatchAsync(CallbackQuery query)
    {
        var prefix = query.Data?.Split(':').FirstOrDefault();
        if (string.IsNullOrEmpty(prefix)) return;

        if (_handlersByPrefix.TryGetValue(prefix, out var handler))
        {
            await handler.HandleAsync(query);
        }
    }
}