using SummyAITelegramBot.Core.Bot.Features.Channel.DTO;
using SummyAITelegramBot.Core.Domain.Models;

namespace SummyAITelegramBot.Core.Bot.Features.Channel.Abstractions;

public interface IPostService
{
    /// <summary>
    /// Обработка поста пришедшего из канала
    /// </summary>
    /// <param name="post"></param>
    /// <returns></returns>
    Task<ChannelPost> AddPostAsync(ChannelPostDto post);

    Task<ChannelPost> UpdatePostAsync(ChannelPostDto post);
}