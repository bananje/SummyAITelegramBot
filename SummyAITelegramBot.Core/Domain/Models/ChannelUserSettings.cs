using SummyAITelegramBot.Core.Abstractions;
using SummyAITelegramBot.Core.Domain.Enums;

namespace SummyAITelegramBot.Core.Domain.Models;

public class ChannelUserSettings : Entity<Guid>
{
    /// <summary>
    /// Время отправки сводок
    /// </summary>
    public TimeOnly? NotificationTime { get; set; }

    /// <summary>
    /// Часовой пояс
    /// </summary>
    public string TimeZoneId { get; set; }

    /// <summary>
    /// Моментально отправлять сводку при выходе поста в канале
    /// </summary>
    public bool? InstantlyTimeNotification { get; set; }

    /// <summary>
    /// Использовать ли медиа в сводке
    /// </summary>
    public bool? MediaEnabled { get; set; }

    /// <summary>
    /// Кому настройка принадлежит
    /// </summary>
    public long UserId { get; set; }

    public User User { get; set; }

    /// <summary>
    /// Язык интерфейса
    /// </summary>
    public Lanquage Language { get; set; } = Lanquage.RU;
}