namespace Payments.Application.Abstractions;

/// <summary>
/// Fast in-memory signal that a payment is ready to process.
/// The database (Status = Pending) remains the source of truth, so nothing is lost on restart.
/// </summary>
public interface IPaymentQueue
{
    ValueTask EnqueueAsync(Guid paymentId, CancellationToken ct);
    IAsyncEnumerable<Guid> DequeueAllAsync(CancellationToken ct);
}