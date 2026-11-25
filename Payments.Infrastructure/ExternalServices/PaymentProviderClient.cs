using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Payments.Application.Abstractions;

namespace Payments.Infrastructure.ExternalServices;

public sealed class PaymentProviderClient(HttpClient httpClient, ILogger<PaymentProviderClient> logger)
    : IPaymentProviderClient
{
    public async Task<ProviderChargeResult> ChargeAsync(ProviderChargeRequest request, CancellationToken ct)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, "api/charges")
        {
            Content = JsonContent.Create(new
            {
                request.PaymentId,
                request.ProviderCode,
                request.Amount,
                request.Currency
            })
        };

        // Our payment ID is the provider-side idempotency key, which makes retries safe
        message.Headers.Add("Idempotency-Key", request.PaymentId.ToString());

        using var response = await httpClient.SendAsync(message, ct);

        if (!response.IsSuccessStatusCode)
        {
            // Reached only after the resilience pipeline has used up its retries
            logger.LogWarning("Provider returned {StatusCode} for payment {PaymentId}",
                (int)response.StatusCode, request.PaymentId);
            return new ProviderChargeResult(false, null, $"Provider error ({(int)response.StatusCode})");
        }

        var body = await response.Content.ReadFromJsonAsync<ProviderChargeResponse>(ct);

        return body?.Status == "Approved"
            ? new ProviderChargeResult(true, body.Reference, null)
            : new ProviderChargeResult(false, null, body?.Reason ?? "Declined by provider");
    }

    private sealed record ProviderChargeResponse(string Status, string? Reference, string? Reason);
}