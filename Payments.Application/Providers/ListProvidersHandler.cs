using Payments.Domain.ProviderAggregate;

namespace Payments.Application.Providers;

public sealed class ListProvidersHandler(IPaymentProviderRepository providers)
{
    public async Task<IReadOnlyList<ProviderDto>> HandleAsync(CancellationToken ct)
    {
        var list = await providers.ListAsync(ct);
        return list.Select(p => new ProviderDto(p.Id, p.Name, p.Code, p.IsActive, p.CreatedAtUtc)).ToList();
    }
}