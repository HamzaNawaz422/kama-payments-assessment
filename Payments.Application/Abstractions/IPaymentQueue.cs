namespace Payments.Application.Abstractions;


public interface IPaymentQueue
{
    ValueTask EnqueueAsync(Guid paymentId, CancellationToken ct);
    IAsyncEnumerable<Guid> DequeueAllAsync(CancellationToken ct);
}