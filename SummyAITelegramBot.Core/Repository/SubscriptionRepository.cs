using Microsoft.EntityFrameworkCore;
using SummyAITelegramBot.Core.Domain.Models;
using SummyAITelegramBot.Core.Utils.Repository;
using SummyAITelegramBot.Infrastructure.Context;

namespace SummyAITelegramBot.Core.Repository;

public class SubscriptionRepository : GenericRepository<Guid, Subscription>
{
    public SubscriptionRepository(AppDbContext context) : base(context)
    {
    }

    public override async Task<Subscription> CreateOrUpdateAsync(Subscription entity, CancellationToken cancellationToken = default)
    {
        var entry = await _context.Set<Subscription>()
            .FirstOrDefaultAsync(u => u.UserId == entity.UserId, cancellationToken);

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
}
