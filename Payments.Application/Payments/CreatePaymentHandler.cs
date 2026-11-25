using System.Security.Cryptography;
using System.Text;
using Payments.Application.Abstractions;
using Payments.Application.Common;
using Payments.Domain.PaymentAggregate;
using Payments.Domain.ProviderAggregate;
using Payments.Domain.ValueObjects;

namespace Payments.Application.Payments;

public sealed record CreatePaymentResult(PaymentDetailsDto Payment, bool IsReplay);

public sealed class CreatePaymentHandler(
    IPaymentRepository payments,
    IPaymentProviderRepository providers,
    IPaymentQueries queries,
    IUnitOfWork unitOfWork,
    IPaymentQueue queue,
    ICurrentUser currentUser)
{
    public async Task<CreatePaymentResult> HandleAsync(
        CreatePaymentRequest request, string? idempotencyKey, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            throw new ValidationException("Idempotency-Key header is required.");

        if (request.Amount <= 0)
            throw new ValidationException("Amount must be greater than zero.");

        if (!Money.IsSupportedCurrency(request.Currency))
            throw new ValidationException($"Currency '{request.Currency}' is not supported.");

        var requestHash = ComputeRequestHash(request);

        // Same key already used by this user → replay or conflict
        var existing = await payments.GetByIdempotencyKeyAsync(currentUser.UserId, idempotencyKey, ct);
        if (existing is not null)
        {
            if (existing.RequestHash != requestHash)
                throw new ConflictException("This Idempotency-Key was already used with a different request.");

            var replay = await queries.GetByIdAsync(existing.Id, ct)
                         ?? throw new NotFoundException("Payment not found.");
            return new CreatePaymentResult(replay, IsReplay: true);
        }

        var provider = await providers.GetByIdAsync(request.ProviderId, ct)
                       ?? throw new NotFoundException("Payment provider not found.");

        if (!provider.IsActive)
            throw new ValidationException("Payment provider is not active.");

        var payment = Payment.Create(
            currentUser.UserId,
            provider.Id,
            Money.Create(request.Amount, request.Currency),
            idempotencyKey,
            requestHash);

        await payments.AddAsync(payment, ct);

        try
        {
            await unitOfWork.SaveChangesAsync(ct);
        }
        catch (DuplicateKeyException)
        {
            // Two parallel requests with the same key: the unique index let only one win
            throw new ConflictException("A request with this Idempotency-Key is already in progress. Retry shortly.");
        }

        // Committed first, then signal the background worker
        await queue.EnqueueAsync(payment.Id, ct);

        var created = await queries.GetByIdAsync(payment.Id, ct)
                      ?? throw new NotFoundException("Payment not found.");
        return new CreatePaymentResult(created, IsReplay: false);
    }

    private static string ComputeRequestHash(CreatePaymentRequest request)
    {
        var raw = $"{request.ProviderId}|{request.Amount:0.00}|{request.Currency.Trim().ToUpperInvariant()}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
    }
}