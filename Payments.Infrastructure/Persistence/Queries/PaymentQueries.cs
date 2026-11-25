using Microsoft.EntityFrameworkCore;
using Payments.Application.Abstractions;
using Payments.Application.Common;
using Payments.Application.Payments;
using Payments.Domain.PaymentAggregate;

namespace Payments.Infrastructure.Persistence.Queries;

public sealed class PaymentQueries(AppDbContext db) : IPaymentQueries
{
    public async Task<PaymentDetailsDto?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var payment = await db.Payments
            .AsNoTracking()
            .Include(p => p.StatusHistory)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (payment is null)
            return null;

        var providerName = await db.PaymentProviders
            .AsNoTracking()
            .Where(pr => pr.Id == payment.ProviderId)
            .Select(pr => pr.Name)
            .FirstOrDefaultAsync(ct) ?? "Unknown";

        var history = payment.StatusHistory
            .OrderBy(h => h.ChangedAtUtc)
            .Select(h => new PaymentStatusChangeDto(
                h.FromStatus?.ToString(), h.ToStatus.ToString(), h.ChangedBy, h.Note, h.ChangedAtUtc))
            .ToList();

        return new PaymentDetailsDto(
            payment.Id, payment.UserId, payment.ProviderId, providerName,
            payment.Amount.Amount, payment.Amount.Currency, payment.Status.ToString(),
            payment.AttemptCount, payment.ProviderReference, payment.FailureReason,
            payment.CreatedAtUtc, payment.UpdatedAtUtc, history);
    }

    public async Task<PagedResult<PaymentSummaryDto>> ListAsync(
        Guid? userId, PaymentStatus? status, int page, int pageSize, CancellationToken ct)
    {
        var query = db.Payments.AsNoTracking();

        if (userId.HasValue)
            query = query.Where(p => p.UserId == userId.Value);

        if (status.HasValue)
            query = query.Where(p => p.Status == status.Value);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(p => p.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Join(db.PaymentProviders, p => p.ProviderId, pr => pr.Id, (p, pr) => new PaymentSummaryDto(
                p.Id, p.UserId, pr.Name, p.Amount.Amount, p.Amount.Currency,
                p.Status.ToString(), p.AttemptCount, p.CreatedAtUtc))
            .ToListAsync(ct);

        return new PagedResult<PaymentSummaryDto>(items, page, pageSize, totalCount);
    }
}