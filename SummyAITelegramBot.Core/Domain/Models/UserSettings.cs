using SummyAITelegramBot.Core.Abstractions;
using SummyAITelegramBot.Core.Domain.Enums;

namespace SummyAITelegramBot.Core.Domain.Models;

public class UserSettings : Entity<Guid>
{
    /// <summary>
    /// Для какого канала настройка
    /// </summary>
    public long ChannelId { get; set; }

    public Channel Channel { get; set; }

    /// <summary>
    /// День в который отправлять сводку
    /// 0 - если в день создания поста
    /// </summary>
    public int Day { get; set; }

    /// <summary>
    /// Время отправки сводок
    /// </summary>
    public TimeOnly? NotificationTime { get; set; }

    /// <summary>
    /// Моментально отправлять сводку при выходе поста в канале
    /// </summary>
    public bool InstantlyTimeNotification { get; set; }

    /// <summary>
    /// Модель ИИ для генерации сводки
    /// </summary>
    public AiModel AiModel { get; set; } = AiModel.DeepSeek;

    // TO:DO добавить рекламу
    // TO:DO отключить дублирование

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
}