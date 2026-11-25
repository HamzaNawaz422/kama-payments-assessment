namespace Payments.Application.Abstractions;


public interface IPaymentProviderClient
{
    Task<ProviderChargeResult> ChargeAsync(ProviderChargeRequest request, CancellationToken ct);
}


public sealed record ProviderChargeRequest(Guid PaymentId, string ProviderCode, decimal Amount, string Currency);

public sealed record ProviderChargeResult(bool Success, string? ProviderReference, string? Error);