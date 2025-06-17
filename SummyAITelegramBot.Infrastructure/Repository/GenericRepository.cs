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

    public virtual IQueryable<TEntity> GetIQueryable()
    {
        return _context.Set<TEntity>().AsQueryable();
    }

    public virtual async Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default)
    {
       return await _context.Set<TEntity>().FindAsync(id, cancellationToken);
    }

    public async Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        _context.Set<TEntity>().Update(entity);;
        return entity;
    }

    public Task<TEntity> GetOrCreateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<TEntity> CreateOrUpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
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
            _context.Update(entry);
            return entry;
        }
    }

    public async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        var entry = await _context.AddAsync(entity);
        return entry.Entity;
    }
}