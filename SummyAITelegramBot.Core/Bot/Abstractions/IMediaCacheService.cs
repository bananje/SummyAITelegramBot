using TL;

namespace SummyAITelegramBot.Core.Bot.Abstractions;

public interface IMediaCacheService
{
    Task<string?> SaveMediaAsync(Message message);
}