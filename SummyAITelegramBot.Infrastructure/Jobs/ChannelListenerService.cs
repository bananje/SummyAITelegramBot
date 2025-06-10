using Microsoft.Extensions.Hosting;
using WTelegram;
using MediatR;
using TL;
using SummyAITelegramBot.Core.Bot.Features.Channel.DTO;
using SummyAITelegramBot.Core.Commands;
using SummyAITelegramBot.Core.Domain.Enums;
using Grpc.Core;

namespace SummyAITelegramBot.Infrastructure.Jobs;

public class ChannelMonitoringService : BackgroundService
{
    private readonly Client _client;
    private readonly ISender _sender;
    private int _pts, _qts, _unreadCount;

    private DateTime _date;

    public ChannelMonitoringService(Client client, ISender sender)
    {
        _client = client;
        _sender = sender;
        // подписываемся на колбэк, чтобы не дублировать логику обработки
        _client.OnUpdates += OnUpdates;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // 1. Авторизуемся (если требуется)
        await _client.LoginUserIfNeeded();

        // 2. Берём текущее состояние (pts, qts, date, unread)
        var state = await _client.Updates_GetState();
        _pts = state.pts;
        _qts = state.qts;
        _unreadCount = state.unread_count;

        // 3. Заходим в цикл long-polling
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Запрашиваем все новые апдейты (включая UpdateNewChannelMessage)
                await _client.Updates_GetDifference(
                    pts: _pts,
                    qts: _qts,
                    date: _date
                );
                // WTelegram.Client обновит внутренние pts/qts/date и вызовет OnUpdates
            }
            catch (Grpc.Core.RpcException ex) when (ex.StatusCode == StatusCode.Internal)
            {
                // Иногда бывает INTERNAL — сбросим состояние
                state = await _client.Updates_GetState();
                _pts = state.pts;
                _qts = state.qts;
                _date = state.date;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ChannelMonitoringService] Poll error: {ex.Message}");
            }

            await Task.Delay(1000, stoppingToken);
        }
    }

    private async Task OnUpdates(UpdatesBase updates)
    {
        // 1) Short — одно Update
        if (updates is UpdateShort us)
        {
            await Handle(us.update);
        }
        // 2) Batch — множество Update
        else if (updates is Updates u)
        {
            var tasks = u.updates.Select(Handle);
            await Task.WhenAll(tasks);
        }
    }

    private async Task Handle(Update upd)
    {
        // Новое сообщение в канале
        if (upd is UpdateNewChannelMessage cnm
            && cnm.message is Message msg
            && msg.peer_id is PeerChannel peer)
        {
            await Process(msg.id, msg.message, peer.channel_id, msg.Date, EntityAction.Create);
        }
        // Редактирование сообщения в канале
        else if (upd is UpdateEditChannelMessage enm
                 && enm.message is Message edited
                 && edited.peer_id is PeerChannel peerEdit)
        {
            await Process(edited.id, edited.message, peerEdit.channel_id, edited.edit_date, EntityAction.Update);
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

        // Шлём команду в MediatR (или ваш ISender) для дальнейшей обработки
        await _sender.Send(new ProcessTelegramChannelPostCommand(dto, action));
    }
}