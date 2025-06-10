using SummyAITelegramBot.Core.Bot.Features.Channel.Abstractions;
using SummyAITelegramBot.Core.Bot.Features.Channel.DTO;
using TL;
using WTelegram;

namespace SummyAITelegramBot.Core.Bot.Features.Channel.Services;

public class TelegramChannelAdapter(Client _client) : ITelegramChannelAdapter
{
    public async Task<List<ChannelPostDto>> GetLatestPostsAsync(string channelUrl)
    {
        var username = channelUrl.Split('/').Last();
        await _client.LoginUserIfNeeded();

        var resolved = await _client.Contacts_ResolveUsername(username);
        if (resolved == null || resolved.chats.Count == 0)
            throw new Exception("Channel not found or inaccessible");

        var channel = resolved.chats.Values.OfType<TL.Channel>().FirstOrDefault();
        if (channel == null)
            throw new Exception("Not a valid channel");

        var history = await _client.Messages_GetHistory(channel, limit: 5);

        var posts = history.Messages
            .OfType<TL.Message>()
            .Where(m => !string.IsNullOrWhiteSpace(m.message))
            .Select(m => new ChannelPostDto
            {
                Text = m.message,
                CreatedAt = DateTimeOffset.FromUnixTimeSeconds(m.date.Second).DateTime
            })
            .ToList();

        return posts;
    }

    public async Task<TL.Channel?> ResolveChannelAsync(string channelUrl)
    {
        if (string.IsNullOrWhiteSpace(channelUrl))
            return null;

        await _client.LoginUserIfNeeded();

        var identifier = ExtractIdentifier(channelUrl);
        if (string.IsNullOrWhiteSpace(identifier))
            return null;

        // Приватные invite‑ссылки
        if (identifier.StartsWith("joinchat/", StringComparison.OrdinalIgnoreCase) ||
            identifier.StartsWith("+"))
        {
            return await ResolvePrivateChannelAsync(identifier);
        }

        // Публичные username
        return await ResolveAndJoinPublicChannelAsync(identifier);
    }

    private static string? ExtractIdentifier(string input)
    {
        if (input?.Trim() is null) { return null; }

        // http(s)://t.me/...
        if (input.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            var uri = new Uri(input);
            return uri.AbsolutePath.TrimStart('/');
        }
        // @username
        if (input.StartsWith("@"))
            return input[1..];
        // +inviteCode
        if (input.StartsWith("+"))
            return input;
        // просто username
        return input;
    }

    private async Task<TL.Channel?> ResolveAndJoinPublicChannelAsync(string username)
    {
        // 1) Resolve username → chats
        var resolved = await _client.Contacts_ResolveUsername(username);
        var channel = resolved?.chats?
            .Values
            .OfType<TL.Channel>()
            .FirstOrDefault();

        if (channel == null)
            return null;  // не нашли такого username

        // 2) Подписываемся (join) — чтобы получать UpdateNewChannelMessage
        try
        {
            var inputPeer = new InputPeerChannel(channel.id, channel.access_hash!);
            await _client.Channels_JoinChannel(inputPeer);
        }
        catch (Exception ex) when (
            ex is RpcException rpc && rpc.Code == 400 && rpc.Message.Contains("CHANNEL_PRIVATE") ||
            ex is RpcException rpc2 && rpc2.Code == 500 // уже подписан
        )
        {
            // либо приватный канал без invite, либо уже в нём — это нормально
        }
        // все остальные ошибки можно зарегистрировать, если нужно

        return channel;
    }

    private async Task<TL.Channel?> ResolvePrivateChannelAsync(string identifier)
    {
        var inviteHash = identifier.Contains("+")
           ? identifier.Split('+').Last()
           : identifier.Split('/').Last();

        var invite = await _client.Messages_CheckChatInvite(inviteHash);

        if (invite is TL.ChatInviteAlready alreadyJoined)
        {
            // уже в канале → найдём его среди диалогов
            var dialogs = await _client.Messages_GetAllDialogs();
            return dialogs.chats
                .Values
                .OfType<TL.Channel>()
                .FirstOrDefault(ch => ch.title == alreadyJoined.chat.Title);
        }
        else if (invite is TL.ChatInvite)
        {
            // ещё не в канале → импортируем
            var imported = await _client.Messages_ImportChatInvite(inviteHash);
            return imported
                .Chats
                .Values
                .OfType<TL.Channel>()
                .FirstOrDefault();
        }

        return null;
    }
}