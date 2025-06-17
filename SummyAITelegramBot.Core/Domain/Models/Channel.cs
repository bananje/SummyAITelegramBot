using SummyAITelegramBot.Core.Abstractions;

namespace SummyAITelegramBot.Core.Domain.Models;

public class Channel : Entity<long>
{
    public string Link { get; set; } = string.Empty;

    public bool IsPrivate { get; set; }

    /// <summary>
    /// Фейковый или скам-канал
    /// </summary>
    public bool HasStopFactor { get; set; }

    /// <summary>
    /// Добавлен в систему
    /// </summary>
    public DateTime AddedDate { get; set; }

    public ICollection<User> Users { get; set; } = [];  

    public ICollection<ChannelPost> Posts { get; set; } = [];
}