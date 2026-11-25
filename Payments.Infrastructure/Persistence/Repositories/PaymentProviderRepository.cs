using Microsoft.EntityFrameworkCore;
using Payments.Domain.ProviderAggregate;

namespace Payments.Infrastructure.Persistence.Repositories;

public sealed class PaymentProviderRepository(AppDbContext db) : IPaymentProviderRepository
{
    public Task<PaymentProvider?> GetByIdAsync(Guid id, CancellationToken ct)
        => db.PaymentProviders.FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<IReadOnlyList<PaymentProvider>> ListAsync(CancellationToken ct)
        => await db.PaymentProviders.OrderBy(p => p.Name).ToListAsync(ct);

    public Task<bool> CodeExistsAsync(string code, CancellationToken ct)
        => db.PaymentProviders.AnyAsync(p => p.Code == code, ct);

    public async Task AddAsync(PaymentProvider provider, CancellationToken ct)
        => await db.PaymentProviders.AddAsync(provider, ct);
}