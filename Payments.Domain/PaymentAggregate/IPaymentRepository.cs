namespace Payments.Domain.PaymentAggregate;

/// <summary>Write-side repository for the Payment aggregate (one repository per aggregate root).</summary>
public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Payment?> GetByIdempotencyKeyAsync(Guid userId, string idempotencyKey, CancellationToken ct);
    Task AddAsync(Payment payment, CancellationToken ct);

    // Used by the background services
    Task<IReadOnlyList<Guid>> GetPendingIdsAsync(int take, CancellationToken ct);
    Task<IReadOnlyList<Guid>> GetRetryableFailedIdsAsync(int maxAttempts, int take, CancellationToken ct);
    Task<IReadOnlyList<Payment>> GetProcessingAsync(CancellationToken ct);
}