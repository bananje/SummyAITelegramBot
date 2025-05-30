using Microsoft.EntityFrameworkCore;
using SummyAITelegramBot.Core.Abstractions;
using SummyAITelegramBot.Infrastructure.Context;

namespace SummyAITelegramBot.Infrastructure.Repository;

public class GenericRepository<TId, TEntity> : IRepository<TId, TEntity> where TEntity : Entity<TId>
{
    protected readonly AppDbContext _context;

    public GenericRepository(AppDbContext context)
    {
        _context = context;
    }

    public virtual Task<TEntity> CreateOrUpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public virtual async Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default)
    {
       return await _context.Set<TEntity>().FindAsync(id, cancellationToken);
    }
}