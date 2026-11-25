using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Payments.Application.Abstractions;
using Payments.Application.Common;
using Payments.Domain.Common;

namespace Payments.Infrastructure.Persistence;

public sealed class UnitOfWork(AppDbContext db, ILogger<UnitOfWork> logger) : IUnitOfWork
{
    public async Task SaveChangesAsync(CancellationToken ct)
    {
        var aggregates = db.ChangeTracker.Entries<AggregateRoot>()
            .Select(e => e.Entity)
            .Where(a => a.DomainEvents.Count > 0)
            .ToList();

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyException("The record was modified by another process.");
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            throw new DuplicateKeyException("A record with the same unique key already exists.");
        }

        foreach (var aggregate in aggregates)
        {
            foreach (var domainEvent in aggregate.DomainEvents)
                logger.LogInformation("Domain event {EventType}: {@Event}", domainEvent.GetType().Name, domainEvent);

            aggregate.ClearDomainEvents();
        }
    }
}