using Microsoft.Extensions.DependencyInjection;
using SummyAITelegramBot.Core.Abstractions;
using SummyAITelegramBot.Core.Utils;
using System.Reflection;
using Telegram.Bot.Types;

namespace SummyAITelegramBot.Core.Common;

public class CommandFactory : ICommandFactory
{
    private readonly IServiceScopeFactory _scopeFactory;
    private static readonly Dictionary<string, Type> _handlers = new();

    public CommandFactory(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
        LoadHandlers();
    }

    private void LoadHandlers()
    {
        var handlerTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => t.GetCustomAttribute<CommandHandlerAttribute>() != null);

        foreach (var type in handlerTypes)
        {
            var attr = type.GetCustomAttribute<CommandHandlerAttribute>();
            _handlers[attr!.CommandName] = type;
        }
    }

    public async Task ProcessCommandAsync(string command, Message message)
    {
        var normalizedCommand = NormalizeCommand(command);

        if (_handlers.TryGetValue(normalizedCommand, out var handlerType))
        {
            using var scope = _scopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<IMessageHandler>();

            await handler!.HandleAsync(message);
        }
    }

    private static string NormalizeCommand(string command)
    {
        return command.TrimStart('/')
            .ToLowerInvariant();
    }
}