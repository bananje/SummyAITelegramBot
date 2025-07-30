namespace SummyAITelegramBot.Core.Abstractions;

public interface IRepository<TId, TEntity>
{
    /// <summary>
    /// Получить сущность по ID
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Удалить сущность
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task RemoveAsync(TId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Удалить сущности
    /// </summary>
    /// <param name="ids">IDs сущностей</param>
    /// <returns></returns>
    Task RemoveRangeAsync(IEnumerable<TId> ids, CancellationToken cancellationToken = default);

    /// <summary>
    /// Обновляет сущность.
    /// </summary>
    /// <param name="entity">Сущность для добавления</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Обновить сущности
    /// </summary>
    /// <param name="entities"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<List<TEntity>> UpdateRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    /// <summary>
    /// Создать сущность
    /// </summary>
    /// <returns></returns>
    Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    Task<TEntity> CreateOrUpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Для нетиповых запросов
    /// </summary>
    /// <returns></returns>
    IQueryable<TEntity> GetIQueryable();
}