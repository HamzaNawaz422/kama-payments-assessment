using Payments.Application.Abstractions;
using Payments.Application.Common;
using Payments.Domain.ProviderAggregate;

namespace Payments.Application.Providers;

public sealed class UpdateProviderHandler(IPaymentProviderRepository providers, IUnitOfWork unitOfWork)
{
    public async Task<ProviderDto> HandleAsync(Guid id, UpdateProviderRequest request, CancellationToken ct)
    {
        var provider = await providers.GetByIdAsync(id, ct)
                       ?? throw new NotFoundException("Payment provider not found.");

        provider.Rename(request.Name);

        if (request.IsActive) provider.Activate();
        else provider.Deactivate();

        await unitOfWork.SaveChangesAsync(ct);

        return new ProviderDto(provider.Id, provider.Name, provider.Code, provider.IsActive, provider.CreatedAtUtc);
    }
}