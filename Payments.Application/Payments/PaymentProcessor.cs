using Microsoft.Extensions.Logging;
using Payments.Application.Abstractions;
using Payments.Application.Common;
using Payments.Domain.PaymentAggregate;
using Payments.Domain.ProviderAggregate;

namespace Payments.Application.Payments;

public sealed class PaymentProcessor(
    IPaymentRepository payments,
    IPaymentProviderRepository providers,
    IPaymentProviderClient providerClient,
    IUnitOfWork unitOfWork,
    ILogger<PaymentProcessor> logger)
{
    public async Task ProcessAsync(Guid paymentId, CancellationToken ct)
    {
        var payment = await payments.GetByIdAsync(paymentId, ct);
        if (payment is null)
        {
            logger.LogWarning("Payment {PaymentId} not found, skipping", paymentId);
            return;
        }

        if (payment.Status is PaymentStatus.Completed or PaymentStatus.Processing ||
            (payment.Status == PaymentStatus.Failed && !payment.CanBeAutoRetried))
        {
            logger.LogInformation("Payment {PaymentId} in status {Status}, skipping", paymentId, payment.Status);
            return;
        }

        payment.StartProcessing();
        try
        {
            await unitOfWork.SaveChangesAsync(ct);
        }
        catch (ConcurrencyException)
        {
            logger.LogInformation("Payment {PaymentId} was claimed by another worker", paymentId);
            return;
        }

        var provider = await providers.GetByIdAsync(payment.ProviderId, ct);

        ProviderChargeResult result;
        try
        {
            result = await providerClient.ChargeAsync(
                new ProviderChargeRequest(payment.Id, provider?.Code ?? "UNKNOWN",
                    payment.Amount.Amount, payment.Amount.Currency), ct);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Provider call failed for payment {PaymentId}", paymentId);
            result = new ProviderChargeResult(false, null, "Payment provider unavailable");
        }

        if (result.Success && !string.IsNullOrWhiteSpace(result.ProviderReference))
            payment.MarkCompleted(result.ProviderReference);
        else
            payment.MarkFailed(result.Error ?? "Declined by provider");

        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Payment {PaymentId} finished with status {Status} (attempt {Attempt})",
            paymentId, payment.Status, payment.AttemptCount);
    }
}