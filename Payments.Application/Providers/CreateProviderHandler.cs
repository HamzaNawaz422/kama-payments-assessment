using Payments.Application.Abstractions;
using Payments.Application.Common;
using Payments.Domain.ProviderAggregate;

namespace Payments.Application.Providers;

public sealed class CreateProviderHandler(IPaymentProviderRepository providers, IUnitOfWork unitOfWork)
{
    public async Task<ProviderDto> HandleAsync(CreateProviderRequest request, CancellationToken ct)
    {
        var provider = PaymentProvider.Create(request.Name, request.Code);

        if (await providers.CodeExistsAsync(provider.Code, ct))
            throw new ConflictException($"A provider with code '{provider.Code}' already exists.");

        await providers.AddAsync(provider, ct);

        try
        {
            await unitOfWork.SaveChangesAsync(ct);
        }
        catch (DuplicateKeyException)
        {
            throw new ConflictException($"A provider with code '{provider.Code}' already exists.");
        }

        return new ProviderDto(provider.Id, provider.Name, provider.Code, provider.IsActive, provider.CreatedAtUtc);
    }
}