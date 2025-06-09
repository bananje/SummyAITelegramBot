using SummyAITelegramBot.Core.Bot.Features.Channel.Abstractions;
using SummyAITelegramBot.Core.Bot.Features.Channel.DTO;
using ChannelDomainEn = SummyAITelegramBot.Core.Domain.Models.Channel;
using TL;
using WTelegram;

namespace SummyAITelegramBot.Core.Bot.Features.Channel.Services;

public class ChannelService : IChannelService
{
    private readonly Client _client;

    public ChannelService()
    {
        _client = new Client(Config);
    }

    private static string Config(string what)
    {
        return what switch
        {
            "api_id" => "28909018",                   // Твой api_id из Telegram
            "api_hash" => "e2ddd24db858eefbf3c2434b895a40cf", // Твой api_hash из Telegram
            "phone_number" => "+79183207444",       // Номер телефона (только при первом запуске) // Код из SMS/Telegram при первом входе    // Пароль, если включена двухфакторка
            _ => null
        };
    }

    public async Task<List<ChannelPost>> GetLatestPostsAsync(string channelUrl)
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
            .Select(m => new ChannelPost
            {
                Text = m.message,
                Date = DateTimeOffset.FromUnixTimeSeconds(m.date.Second).DateTime
            })
            .ToList();

        return posts;
    }

    //public async Task<ChannelDomainEn?> CreateAndResolveChannelAsync(string channelUrl)
    //{
    //    await _client.LoginUserIfNeeded();

    //    var uri = CreateUri(channelUrl);

    //    if (uri is null) { return null; }

    //    if (!uri.StartsWith("joinchat/", StringComparison.OrdinalIgnoreCase))
    //    {
    //        // Попытка разрешить по username
    //        var resolved = await _client.Contacts_ResolveUsername(uri);
    //        if (resolved?.chats != null && resolved.chats.Count > 0)
    //        {
    //            // Берём канал
    //            var channel = resolved.chats.Values.OfType<TL.Channel>().FirstOrDefault();

    //            cha
    //            return channel;ы
    //        }
    //        return null;
    //    }
    //}

    private string? CreateUri(string channelUrl)
    {
        var uri = channelUrl.Trim();

        if (uri.StartsWith("https://t.me/", StringComparison.OrdinalIgnoreCase))
            uri = uri.Substring("https://t.me/".Length);

        else if (uri.StartsWith("http://t.me/", StringComparison.OrdinalIgnoreCase))
            uri = uri.Substring("http://t.me/".Length);

        if (string.IsNullOrWhiteSpace(uri))
            return null;

        return uri;
    }
}
