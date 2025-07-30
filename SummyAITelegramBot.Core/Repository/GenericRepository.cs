using Microsoft.EntityFrameworkCore;
using SummyAITelegramBot.Core.Abstractions;
using SummyAITelegramBot.Infrastructure.Context;

namespace SummyAITelegramBot.Core.Utils.Repository;

public class GenericRepository<TId, TEntity> : IRepository<TId, TEntity> where TEntity : Entity<TId>
{
    protected readonly AppDbContext _context;

    public GenericRepository(AppDbContext context)
    {
        _context = context;
    }

    public virtual IQueryable<TEntity> GetIQueryable()
    {
        return _context.Set<TEntity>().AsQueryable();
    }

    public virtual async Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<TEntity>().FindAsync(id, cancellationToken);
    }

    public virtual async Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        _context.Set<TEntity>().Update(entity); ;
        return entity;
    }

    public virtual async Task<List<TEntity>> UpdateRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        _context.Set<TEntity>().UpdateRange(entities); ;
        return entities.ToList();
    }

    public virtual Task<TEntity> GetOrCreateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public virtual async Task<TEntity> CreateOrUpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        var entry = await _context.Set<TEntity>().FindAsync(new object[] { entity.Id }, cancellationToken);

        if (entry is null)
        {
            var added = (await _context.AddAsync(entity, cancellationToken)).Entity;
            return added;
        }
        else
        {
            _context.Entry(entry).CurrentValues.SetValues(entity);

            return entry;
        }
    }

    public virtual async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        var entry = await _context.AddAsync(entity);
        return entry.Entity;
    }

    public virtual async Task RemoveAsync(TId id, CancellationToken cancellationToken = default)
    {
        var entry = await _context.Set<TEntity>().FindAsync(new object[] { id }, cancellationToken);

        if (entry is not null)
        {
            _context.Remove(entry);
        }
    }

    public virtual async Task RemoveRangeAsync(IEnumerable<TId> ids, CancellationToken cancellationToken = default)
    {
        var entities = await _context.Set<TEntity>()
            .Where(e => ids.Contains(e.Id))
            .ToListAsync(cancellationToken);

        if (entities.Any())
        {
            _context.RemoveRange(entities);
        }
    }
}