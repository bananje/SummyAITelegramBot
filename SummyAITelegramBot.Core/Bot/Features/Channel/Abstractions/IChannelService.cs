using SummyAITelegramBot.Core.Bot.Features.Channel.DTO;

namespace SummyAITelegramBot.Core.Bot.Features.Channel.Abstractions;

public interface IChannelService
{
    Task<List<ChannelPost>> GetLatestPostsAsync(string channelUrl);
}
