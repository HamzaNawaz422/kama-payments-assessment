namespace Payments.Application.Abstractions;

public interface IUnitOfWork
{
    /// <summary>Saves all changes in one transaction. Throws ConcurrencyException on a RowVersion conflict.</summary>
    Task SaveChangesAsync(CancellationToken ct);
}