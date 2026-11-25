namespace Payments.Application.Abstractions;

/// <summary>Port to the external payment provider (Service B). Implemented with a typed HttpClient.</summary>
public interface IPaymentProviderClient
{
    Task<ProviderChargeResult> ChargeAsync(ProviderChargeRequest request, CancellationToken ct);
}

/// <summary>PaymentId is sent as the provider-side idempotency key, so a repeated call is safe.</summary>
public sealed record ProviderChargeRequest(Guid PaymentId, string ProviderCode, decimal Amount, string Currency);

public sealed record ProviderChargeResult(bool Success, string? ProviderReference, string? Error);