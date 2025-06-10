using SummyAITelegramBot.Core.Bot.Features.Channel.DTO;
using WTelegram;

namespace SummyAITelegramBot.Core.Bot.Features.Channel.Abstractions;

public interface ITelegramChannelAdapter
{
    Task<List<ChannelPostDto>> GetLatestPostsAsync(string channelUrl);

    Task<TL.Channel?> ResolveChannelAsync(string channelUrl);
}