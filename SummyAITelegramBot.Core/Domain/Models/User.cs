using SummyAITelegramBot.Core.Abstractions;

namespace SummyAITelegramBot.Core.Domain.Models;

public class User : Entity<long>
{
    public ICollection<UserSettings> UserSettings { get; set; } = []; // Настройки пользователя

    public ICollection<Channel> Channels { get; set; } = []; // Каналы, в которых состоит пользователь

    public long ChatId { get; set; }

    // Основные данные пользователя Telegram
    public long TelegramId { get; set; }                    // Telegram User ID
    public string? FirstName { get; set; }                  // Имя
    public string? LastName { get; set; }                   // Фамилия
    public string? Username { get; set; }                   // @username
    public string? LanguageCode { get; set; }               // Язык интерфейса пользователя (ru, en и т.п.)
    public bool? IsPremium { get; set; }                    // Telegram Premium

    // Контактные данные (только если пользователь дал разрешение)
    public string? PhoneNumber { get; set; }                // Телефон (через кнопку "Поделиться контактом")

    // Геолокация (только если пользователь дал разрешение)
    public float? Latitude { get; set; }                    // Широта
    public float? Longitude { get; set; }                   // Долгота

    // Дата последнего взаимодействия
    public DateTime? LastInteractionAt { get; set; }         // Когда последний раз обращался к боту

    // Дополнительно (если нужно вести аналитику)
    public bool IsBot { get; set; }                         // Является ли пользователь ботом (редко используется)
    public bool? AddedToAttachmentMenu { get; set; }        // Добавил ли бот в меню вложений (если доступно)

    // Можно добавить флаг для логики
    public bool IsFullyRegistered => !string.IsNullOrEmpty(PhoneNumber); // Пример логики регистрации

    public void AddChannel(Channel channel) => Channels.Add(channel);
}