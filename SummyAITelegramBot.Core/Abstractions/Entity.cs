namespace SummyAITelegramBot.Core.Abstractions;

public abstract class Entity<TIdType>
{
    public TIdType Id { get; set; }
}
