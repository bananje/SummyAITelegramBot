using Microsoft.Extensions.Hosting;
using WTelegram;
using TL;
using SummyAITelegramBot.Core.Bot.Features.Channel.DTO;
using SummyAITelegramBot.Core.Commands;
using SummyAITelegramBot.Core.Domain.Enums;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace SummyAITelegramBot.API.Jobs;

public class ChannelMonitoringService : BackgroundService
{
    private readonly Client _client;
    private readonly IServiceProvider _serviceProvider;

    private int _pts;
    private int _qts;
    private DateTime _date;
    private DateTime _startTimeUtc;

    public ChannelMonitoringService(Client client, IServiceProvider serviceProvider)
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

                await Process(msg.id, msg.message, peer.channel_id, msg.Date, EntityAction.Create);
                break;

            case UpdateEditChannelMessage enm when enm.message is Message edited && edited.peer_id is PeerChannel peerEdit:
                var editDate = edited.edit_date.ToUniversalTime();
                if (editDate < _startTimeUtc)
                    return;

                await Process(edited.id, edited.message, peerEdit.channel_id, edited.edit_date, EntityAction.Update);
                break;
        }
    }

    private async Task Process(int id, string text, long channelId, DateTime timeUtc, EntityAction action)
    {
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
                : null
        };

        using var scope = _serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        await mediator.Send(new ProcessTelegramChannelPostCommand(dto, action));
    }
}