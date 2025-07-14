using Microsoft.EntityFrameworkCore;
using SummyAITelegramBot.Core.Abstractions;
using SummyAITelegramBot.Core.Bot.Features.Channel.Abstractions;
using SummyAITelegramBot.Core.Bot.Features.Channel.DTO;
using SummyAITelegramBot.Core.Domain.Models;
using SummyAITelegramBot.Infrastructure.Context;
using ChannelEn = SummyAITelegramBot.Core.Domain.Models.Channel;

namespace SummyAITelegramBot.Core.Bot.Features.Channel.Services;

public class PostService(IUnitOfWork unitOfWork, AppDbContext context) : IPostService
{
    public async Task<ChannelPost> AddPostAsync(ChannelPostDto postDto)
    {
        var postsRepository = unitOfWork.Repository<int, ChannelPost>();
        var channelsRepository = unitOfWork.Repository<long, ChannelEn>();

        var channel = await channelsRepository.GetByIdAsync(postDto.ChannelId)
            ?? throw new Exception($"В системе не зарегистриван канал с ID: {postDto.ChannelId}");

        var post = new ChannelPost
        {
            Id = postDto.Id,
            ChannelId = postDto.ChannelId,
            CreatedDate = postDto.CreatedAt,
            Text = postDto.Text,
            MediaPath = postDto.MediaPath
        };

        var entry = await context.Set<ChannelPost>().
            FirstOrDefaultAsync(p => p.ChannelId == postDto.ChannelId && p.Id == postDto.Id);

        if (entry is null)
        {
            entry = (await context.AddAsync(post)).Entity;
        }
        else
        {
            context.Entry(entry).CurrentValues.SetValues(post);
        }

        await unitOfWork.CommitAsync();

        return entry;
    }

    public async Task<ChannelPost> UpdatePostAsync(ChannelPostDto postDto)
    {
        var postsRepository = unitOfWork.Repository<int, ChannelPost>();
        var channelsRepository = unitOfWork.Repository<long, ChannelEn>();

        var channel = await channelsRepository.GetByIdAsync(postDto.ChannelId)
            ?? throw new Exception($"В системе не зарегистриван канал с ID: {postDto.ChannelId}");

        var post = await postsRepository.GetIQueryable()
             .FirstOrDefaultAsync(p => p.ChannelId == postDto.ChannelId && p.Id == postDto.Id)
                ?? throw new Exception($"Пост не найден ID поста: {postDto.Id}, Id канала: {postDto.ChannelId}");

        post.Text = postDto.Text;

        var result = await postsRepository.UpdateAsync(post);

        await unitOfWork.CommitAsync();

        return result;
    }
}