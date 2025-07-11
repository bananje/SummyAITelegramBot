using Microsoft.Extensions.Caching.Memory;
using SummyAITelegramBot.Core.Bot.Abstractions;

namespace SummyAITelegramBot.Core.Bot.Utils;

public class UserCommandCache : IUserCommandCache
{
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _expiration = TimeSpan.FromSeconds(10);

    public UserCommandCache(IMemoryCache cache)
    {
        _cache = cache;
    }

    public void SetLastCommand(long userId, string command)
    {
        _cache.Set(GetKey(userId), command, _expiration);
    }

    public string? GetLastCommand(long userId)
    {
        _cache.TryGetValue(GetKey(userId), out string? command);
        return command;
    }

    private string GetKey(long userId) => $"last_command_{userId}";
}