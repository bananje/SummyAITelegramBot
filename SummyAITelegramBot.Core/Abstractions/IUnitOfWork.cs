namespace SummyAITelegramBot.Core.Abstractions;

public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// Возвращает репозиторий для работы с сущностями.
    /// </summary>
    IRepository<TId ,TEntity> Repository<TId, TEntity>() where TEntity : Entity<TId>;

    /// <summary>
    /// Сохраняет все изменения, внесённые в контекст.
    /// </summary>
    /// <returns>Количество затронутых записей</returns>
    Task<int> CommitAsync(CancellationToken cancellationToken = default);
}