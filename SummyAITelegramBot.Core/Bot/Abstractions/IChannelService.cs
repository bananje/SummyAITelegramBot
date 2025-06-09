using SummyAITelegramBot.Core.Bot.Features.Channel.DTO;

namespace SummyAITelegramBot.Core.Bot.Abstractions;

internal interface IChannelService
{
    Task<List<ChannelPost>> GetLatestPostsAsync(string channelUrl);
}
