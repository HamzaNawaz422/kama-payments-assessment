namespace Payments.Domain.ProviderAggregate;

public interface IPaymentProviderRepository
{
    Task<PaymentProvider?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<PaymentProvider>> ListAsync(CancellationToken ct);
    Task<bool> CodeExistsAsync(string code, CancellationToken ct);
    Task AddAsync(PaymentProvider provider, CancellationToken ct);
}