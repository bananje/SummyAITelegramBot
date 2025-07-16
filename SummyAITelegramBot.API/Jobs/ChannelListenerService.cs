using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Serilog;
using SummyAITelegramBot.Core.Abstractions;
using SummyAITelegramBot.Core.Bot.Abstractions;
using SummyAITelegramBot.Core.Bot.Features.Channel.DTO;
using SummyAITelegramBot.Core.Commands;
using SummyAITelegramBot.Core.Domain.Enums;
using SummyAITelegramBot.Infrastructure.Context;
using System.IO;
using TL;
using WTelegram;

namespace SummyAITelegramBot.API.Jobs;

public class ChannelMonitoringWorker : BackgroundService
{
    private readonly Client _client;
    private readonly IServiceProvider _serviceProvider;

    private int _pts;
    private int _qts;
    private DateTime _date;
    private DateTime _startTimeUtc;

    public ChannelMonitoringWorker(
        Client client, IServiceProvider serviceProvider)
    {
        _client = client;
        _serviceProvider = serviceProvider;

        // Подписка на входящие обновления
        _client.OnUpdates += OnUpdate;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // 1. Авторизация (если нужно)
        await _client.LoginUserIfNeeded();

        // 2. Получаем актуальное состояние Telegram (но НЕ загружаем backlog)
        var state = await _client.Updates_GetState();
        _pts = state.pts;
        _qts = state.qts;
        _date = state.date;

        // 3. Устанавливаем точку отсечения — только live-сообщения после этой даты
        _startTimeUtc = DateTime.UtcNow;

        // 4. Просто держим сервис живым
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task OnUpdate(UpdatesBase updates)
    {
        if (updates is UpdateShort us)
        {
            await Handle(us.update);
        }
        else if (updates is Updates u)
        {
            var tasks = u.updates.Select(Handle);
            await Task.WhenAll(tasks);
        }
    }

    private async Task Handle(Update upd)
    {
        switch (upd)
        {
            case UpdateNewChannelMessage cnm when cnm.message is Message msg && msg.peer_id is PeerChannel peer:
                if (msg.Date.ToUniversalTime() < _startTimeUtc)
                    return;

                await Process(msg, msg.id, msg.message, peer.channel_id, msg.Date, EntityAction.Create);
                break;

            case UpdateEditChannelMessage enm when enm.message is Message edited && edited.peer_id is PeerChannel peerEdit:
                var editDate = edited.edit_date.ToUniversalTime();
                if (editDate < _startTimeUtc)
                    return;

                await Process(edited, edited.id, edited.message, peerEdit.channel_id, edited.edit_date, EntityAction.Update);
                break;
        }
    }

    private async Task Process(Message message, int id, string text, long channelId, DateTime timeUtc, EntityAction action)
    {
        using var scope = _serviceProvider.CreateScope();
        string path = "";

        try
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var channel = await dbContext.Set<Core.Domain.Models.Channel>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == channelId)
                        ?? throw new Exception($"В системе не зарегистриван канал с ID: {channelId}");

            var mediaCacheService = scope.ServiceProvider.GetRequiredService<IMediaCacheService>();
            var mediaPath = await mediaCacheService.SaveMediaAsync(message);

            var dto = new ChannelPostDto
            {
                Id = id,
                Text = text,
                ChannelId = channelId,
                CreatedAt = action == EntityAction.Create
                    ? DateTime.SpecifyKind(timeUtc, DateTimeKind.Utc)
                    : default,
                UpdatedAt = action == EntityAction.Update
                    ? DateTime.SpecifyKind(timeUtc, DateTimeKind.Utc)
                    : null,
                MediaPath = mediaPath
            };
            path = mediaPath;

            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            await mediator.Send(new ProcessTelegramChannelPostCommand(dto, action));
            DeleteImage(scope, path);
        }
        catch (Exception ex)
        {
            DeleteImage(scope, path);
            scope.Dispose();
            Log.Error(ex, ex.Message);
        }
    }

    private void DeleteImage(IServiceScope scope, string path)
    {
        var staticImageService = scope.ServiceProvider.GetRequiredService<IStaticImageService>();

        staticImageService.DeleteImage(path, "media_cache");
    }
}