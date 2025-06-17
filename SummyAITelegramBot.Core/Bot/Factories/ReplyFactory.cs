using Microsoft.Extensions.DependencyInjection;
using SummyAITelegramBot.Core.Bot.Abstractions;
using SummyAITelegramBot.Core.Bot.Attributes;
using System.Reflection;
using Telegram.Bot.Types;

namespace SummyAITelegramBot.Core.Bot.Factories;

public class ReplyFactory : IReplyFactory
{
    private readonly Dictionary<string, IReplyHandler> _handlersByPrefix;

    public ReplyFactory(IServiceScopeFactory scopeFactory)
    {
        _handlersByPrefix = new();

        var handlerTypes = AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(x => x.GetTypes())
            .Where(t => typeof(IReplyHandler).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

        foreach (var type in handlerTypes)
        {
            var attr = type.GetCustomAttribute<ReplyHandlerAttribute>();
            if (attr != null)
            {
                var scope = scopeFactory.CreateScope();
                var handler = (IReplyHandler)scope.ServiceProvider.GetRequiredService(type);
                _handlersByPrefix[attr.CommandName] = handler;
            }
        }
    }

    public async Task DispatchAsync(Message replyMessage)
    {
        var prefix = replyMessage.Text.ToLower();
        if (string.IsNullOrEmpty(prefix)) return;

        if (_handlersByPrefix.TryGetValue(prefix, out var handler))
        {
            await handler.HandleAsync(replyMessage);
        }
    }
}