using SummyAITelegramBot.Core.Abstractions;
using SummyAITelegramBot.Core.Domain.Enums;

namespace SummyAITelegramBot.Core.Domain.Models;

public class UserSettings : Entity<long>
{
    /// <summary>
    /// Глобальная настройках для всех каналов
    /// </summary>
    public bool IsGlobal { get; set; }

    /// <summary>
    /// Использовать ли медиа в сводке
    /// </summary>
    public bool MediaEnabled { get; set; }

    /// <summary>
    /// Кому настройка принадлежит
    /// </summary>
    public long UserId { get; set; }

    public User User { get; set; }

    /// <summary>
    /// Язык интерфейса
    /// </summary>
    public Lanquage Language { get; set; } = Lanquage.RU;

    /// <summary>
    /// Блокировать похожие посты в рамках каналов
    /// </summary>
    public bool IsBlockingSimilarPostsInChannels { get; set; }

    /// <summary>
    /// Время отправки сводок
    /// </summary>
    public TimeOnly NotificationTime { get; set; }
}