using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Serilog;
using SummyAITelegramBot.Core.Abstractions;
using SummyAITelegramBot.Core.Bot.Abstractions;
using SummyAITelegramBot.Core.Bot.Features.Channel.DTO;
using SummyAITelegramBot.Core.Commands;
using SummyAITelegramBot.Core.Domain.Enums;
using SummyAITelegramBot.Infrastructure.Context;
using TL;
using WTelegram;

namespace SummyAITelegramBot.API.Jobs;

public class ChannelMonitoringWorker : BackgroundService
{
    private readonly Client _client;
    private readonly IServiceProvider _serviceProvider;
    private readonly IMemoryCache _cache;   

    private int _pts;
    private int _qts;
    private DateTime _date;
    private DateTime _startTimeUtc;

    public ChannelMonitoringWorker(
        IMemoryCache cache,
        Client client, IServiceProvider serviceProvider)
    {
        _client = client;
        _serviceProvider = serviceProvider;
        _cache = cache;

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

        var handledPostCacheKey = $"ChannelPost_{channelId}_{id}";

        // Пропускаем повторную обработку
        if (_cache.TryGetValue(handledPostCacheKey, out _))
            return;

        _cache.Set(handledPostCacheKey, new object(), TimeSpan.FromSeconds(20));

        try
        {
            // Пропускаем сообщения без текста
            if (string.IsNullOrWhiteSpace(text))
                return;

            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var channel = await dbContext.Set<Core.Domain.Models.Channel>()
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == channelId);

            if (channel is null)
            {
                Log.Warning($"В системе не зарегистрирован канал с ID: {channelId}");
                return;
            }

            // Медиа не сохраняем
            string? mediaPath = null;

            var dto = new ChannelPostDto
            {
                Id = id,
                Text = text.Trim(),
                ChannelId = channelId,
                CreatedAt = action == EntityAction.Create
                    ? DateTime.SpecifyKind(timeUtc, DateTimeKind.Utc)
                    : default,
                UpdatedAt = action == EntityAction.Update
                    ? DateTime.SpecifyKind(timeUtc, DateTimeKind.Utc)
                    : null,
                MediaPath = null // Указываем явно, что медиа нет
            };

            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            await mediator.Send(new ProcessTelegramChannelPostCommand(dto, action));

            _cache.Set(handledPostCacheKey, dto, TimeSpan.FromSeconds(20));
        }
        catch (Exception ex)
        {
            Log.Error(ex, ex.Message);
        }
    }

    //private async Task Process(Message message, int id, string text, long channelId, DateTime timeUtc, EntityAction action)
    //{
    //    using var scope = _serviceProvider.CreateScope();
    //    string path = "";

    //    var handledPostCacheKey = $"ChannelPost_{channelId}_{id}";

    //    // 💡 Пропускаем повторную обработку
    //    if (_cache.TryGetValue(handledPostCacheKey, out _))
    //        return;

    //    // 💡 Альбом: если уже обработан — пропустить
    //    if (message.grouped_id is long groupId)
    //    {
    //        var albumHandledKey = $"AlbumHandled_{channelId}_{groupId}";

    //        // Если уже есть — это второе+ медиа в альбоме → пропускаем
    //        if (_cache.TryGetValue(albumHandledKey, out _))
    //            return;

    //        // Устанавливаем флаг, что альбом обработан
    //        _cache.Set(albumHandledKey, true, TimeSpan.FromSeconds(30));
    //    }

    //    _cache.Set(handledPostCacheKey, new object(), TimeSpan.FromSeconds(20));

    //    try
    //    {
    //        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    //        var channel = await dbContext.Set<Core.Domain.Models.Channel>()
    //                .AsNoTracking()
    //                .FirstOrDefaultAsync(u => u.Id == channelId);

    //        if (channel is null)
    //        {
    //            Log.Warning($"В системе не зарегистрирован канал с ID: {channelId}");
    //            return;
    //        }

    //        var mediaCacheService = scope.ServiceProvider.GetRequiredService<IMediaCacheService>();
    //        var mediaPath = await mediaCacheService.SaveMediaAsync(message);

    //        var dto = new ChannelPostDto
    //        {
    //            Id = id,
    //            Text = text,
    //            ChannelId = channelId,
    //            CreatedAt = action == EntityAction.Create
    //                ? DateTime.SpecifyKind(timeUtc, DateTimeKind.Utc)
    //                : default,
    //            UpdatedAt = action == EntityAction.Update
    //                ? DateTime.SpecifyKind(timeUtc, DateTimeKind.Utc)
    //                : null,
    //            MediaPath = mediaPath
    //        };

    //        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
    //        await mediator.Send(new ProcessTelegramChannelPostCommand(dto, action));

    //        if (!string.IsNullOrWhiteSpace(mediaPath))
    //        {
    //            path = mediaPath;
    //            DeleteImage(scope, path);
    //        }

    //        _cache.Set(handledPostCacheKey, dto, TimeSpan.FromSeconds(20));
    //    }
    //    catch (Exception ex)
    //    {
    //        if (!string.IsNullOrWhiteSpace(path))
    //        {
    //            DeleteImage(scope, path);
    //        }

    //        Log.Error(ex, ex.Message);
    //    }
    //}

    private void DeleteImage(IServiceScope scope, string path)
    {
        var staticImageService = scope.ServiceProvider.GetRequiredService<IStaticImageService>();

        staticImageService.DeleteImage(path, "media_cache");
    }
}