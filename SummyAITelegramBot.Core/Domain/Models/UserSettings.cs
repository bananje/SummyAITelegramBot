using SummyAITelegramBot.Core.Abstractions;
using SummyAITelegramBot.Core.Domain.Enums;

namespace SummyAITelegramBot.Core.Domain.Models;

public class UserSettings : Entity<long>
{
    /// <summary>
    /// Глобальная настройках для всех каналов
    /// </summary>
    public bool IsGlobal { get; set; }


    public long UserId { get; set; }

    public Lanquage Language { get; set; } = Lanquage.RU;

    public bool? NotificationsEnabled { get; set; }

    public string? TimeZone { get; set; }
}