using SummyAITelegramBot.Core.Domain.Enums;

namespace SummyAITelegramBot.Core.Bot.Utils;

public static class TimeZoneHelper
{
    private static readonly Dictionary<RussianTimeZone, string> _windowsTimeZoneMap = new()
    {
        { RussianTimeZone.Kaliningrad, "Kaliningrad Standard Time" },
        { RussianTimeZone.Moscow, "Russian Standard Time" },
        { RussianTimeZone.Samara, "Russia Time Zone 3" },
        { RussianTimeZone.Yekaterinburg, "N. Central Asia Standard Time" },
        { RussianTimeZone.Omsk, "Omsk Standard Time" },
        { RussianTimeZone.Krasnoyarsk, "North Asia Standard Time" },
        { RussianTimeZone.Irkutsk, "North Asia East Standard Time" },
        { RussianTimeZone.Yakutsk, "Yakutsk Standard Time" },
        { RussianTimeZone.Vladivostok, "Vladivostok Standard Time" },
        { RussianTimeZone.Magadan, "Magadan Standard Time" },
        { RussianTimeZone.Kamchatka, "Kamchatka Standard Time" },
    };

    public static TimeZoneInfo GetTimeZoneInfo(RussianTimeZone zone)
    {
        var id = _windowsTimeZoneMap[zone];
        return TimeZoneInfo.FindSystemTimeZoneById(id);
    }

    public static DateTimeOffset GetCurrentTime(RussianTimeZone zone)
    {
        var tz = GetTimeZoneInfo(zone);
        return TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, tz);
    }

    public static string GetFormattedTime(RussianTimeZone zone, string format = "yyyy-MM-dd HH:mm:ss zzz")
    {
        var time = GetCurrentTime(zone);
        return time.ToString(format);
    }
}