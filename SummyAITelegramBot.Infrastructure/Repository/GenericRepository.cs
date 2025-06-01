using SummyAITelegramBot.Core.Abstractions;
using SummyAITelegramBot.Infrastructure.Context;

namespace SummyAITelegramBot.Infrastructure.Repository;

public class GenericRepository<TId, TEntity> : IRepository<TId, TEntity> where TEntity : class
{
    protected readonly AppDbContext _context;

    public GenericRepository(AppDbContext context)
    {
        _context = context;
    }

    public virtual System.Linq.IQueryable<TEntity> GetIQueryable()
    {
        return _context.Set<TEntity>().AsQueryable();
    }

    public virtual async Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default)
    {
       return await _context.Set<TEntity>().FindAsync(id, cancellationToken);
    }

    public async Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        _context.Set<TEntity>().Update(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public Task<TEntity> GetOrCreateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<TEntity> CreateOrUpdateAsync(TId id, TEntity entity, CancellationToken cancellationToken = default)
    {
        var entry = await _context.Set<TEntity>().FindAsync(id, cancellationToken);

        if (entry is null)
        {
            await _context.AddAsync(entity);
        }
        else
        {
            _context.Set<TEntity>().Update(entity);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }
}