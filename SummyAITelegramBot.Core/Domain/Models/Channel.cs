using SummyAITelegramBot.Core.Abstractions;

namespace SummyAITelegramBot.Core.Domain.Models;

public class Channel : Entity<Guid>
{
    public string Link { get; set; } = string.Empty;

    public bool IsPrivate { get; set; }

    public ICollection<User> Users { get; set; } = [];  
}