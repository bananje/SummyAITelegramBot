﻿using MediatR;
using SummyAITelegramBot.Core.Abstractions;
using SummyAITelegramBot.Core.AI.Abstractions;
using SummyAITelegramBot.Core.Bot.Features.Channel.Abstractions;
using SummyAITelegramBot.Core.Bot.Features.Channel.DTO;
using SummyAITelegramBot.Core.Domain.Enums;

namespace SummyAITelegramBot.Core.Commands;

public record ProcessTelegramChannelPostCommand(ChannelPostDto Post, EntityAction Action) : IRequest;

public class ProcessTelegramChannelPostCommandHandler(
    IPostService postService,
    ISummarizationStrategyFactory aiFactory,
    IStaticImageService staticImageService,
    ITelegramSenderService tgSender) : IRequestHandler<ProcessTelegramChannelPostCommand>
{
    public async Task Handle(ProcessTelegramChannelPostCommand request, CancellationToken cancellationToken)
    {
        var action = request.Action;

        var aiHandler = aiFactory.Create(AiModel.TextHeader);

        //var checkAdversting = await aiHandler.СheckForAdvertising(request.Post.Text);

        //if (!checkAdversting)
        //{
        //    return;
        //}

        var handledByAiText = await aiHandler.SummarizeAsync(request.Post.Text);       

        request.Post.Text = handledByAiText;

        var post = action is EntityAction.Create ? await postService.AddPostAsync(request.Post) 
            : await postService.UpdatePostAsync(request.Post);
      
        await tgSender.ResolveNotifyUsersAsync(post);       
    }
}