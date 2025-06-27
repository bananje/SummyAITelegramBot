using SummyAITelegramBot.Core.Abstractions;
using SummyAITelegramBot.Core.Utils.Repository;
using SummyAITelegramBot.Infrastructure.Context;

namespace SummyAITelegramBot.Infrastructure.Persistence;

public sealed class UnitOfWork(AppDbContext Context) : IUnitOfWork
{
    private Dictionary<Type, object> _repositories;

    /// <summary>
    /// Получает экземпляр репозитория для заданного типа сущности.
    /// </summary>
    public IRepository<TId, TEntity> Repository<TId, TEntity>() where TEntity : Entity<TId>
    {
        _repositories ??= new Dictionary<Type, object>();

        var type = typeof(TEntity);
        if (_repositories.TryGetValue(type, out var value))
        {
            return (IRepository<TId, TEntity>)value;
        }

        var repositoryInstance = new GenericRepository<TId, TEntity>(Context);
        _repositories.Add(type, repositoryInstance);
        return (IRepository<TId, TEntity>)_repositories[type];
    }

    /// <summary>
    /// Сохраняет все изменения в контексте базы данных и публикует доменные события.
    /// </summary>
    public async Task<int> CommitAsync(CancellationToken cancellationToken)
    {
        var result = await Context.SaveChangesAsync(cancellationToken);
     
        return result;
    }

    /// <summary>
    /// Освобождает ресурсы контекста.
    /// </summary>
    public void Dispose()
    {
        Context.Dispose();
    }
}