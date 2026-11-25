using Microsoft.EntityFrameworkCore;
using Payments.Domain.PaymentAggregate;

namespace Payments.Infrastructure.Persistence.Repositories;

public sealed class PaymentRepository(AppDbContext db) : IPaymentRepository
{
    public Task<Payment?> GetByIdAsync(Guid id, CancellationToken ct)
        => db.Payments
            .Include(p => p.StatusHistory)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<Payment?> GetByIdempotencyKeyAsync(Guid userId, string idempotencyKey, CancellationToken ct)
        => db.Payments.FirstOrDefaultAsync(
            p => p.UserId == userId && p.IdempotencyKey == idempotencyKey, ct);

    public async Task AddAsync(Payment payment, CancellationToken ct)
        => await db.Payments.AddAsync(payment, ct);

    public async Task<IReadOnlyList<Guid>> GetPendingIdsAsync(int take, CancellationToken ct)
        => await db.Payments
            .Where(p => p.Status == PaymentStatus.Pending)
            .OrderBy(p => p.CreatedAtUtc)
            .Select(p => p.Id)
            .Take(take)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Guid>> GetRetryableFailedIdsAsync(int maxAttempts, int take, CancellationToken ct)
        => await db.Payments
            .Where(p => p.Status == PaymentStatus.Failed && p.AttemptCount < maxAttempts)
            .OrderBy(p => p.UpdatedAtUtc)
            .Select(p => p.Id)
            .Take(take)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Payment>> GetProcessingAsync(CancellationToken ct)
        => await db.Payments
            .Include(p => p.StatusHistory)
            .Where(p => p.Status == PaymentStatus.Processing)
            .ToListAsync(ct);
}