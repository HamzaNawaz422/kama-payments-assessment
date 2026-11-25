namespace Payments.Application.Payments;

public sealed record CreatePaymentRequest(Guid ProviderId, decimal Amount, string Currency);

public sealed record PaymentSummaryDto(
    Guid Id, Guid UserId, string ProviderName, decimal Amount, string Currency,
    string Status, int AttemptCount, DateTime CreatedAtUtc);

public sealed record PaymentStatusChangeDto(
    string? FromStatus, string ToStatus, string ChangedBy, string? Note, DateTime ChangedAtUtc);

public sealed record PaymentDetailsDto(
    Guid Id, Guid UserId, Guid ProviderId, string ProviderName, decimal Amount, string Currency,
    string Status, int AttemptCount, string? ProviderReference, string? FailureReason,
    DateTime CreatedAtUtc, DateTime UpdatedAtUtc, IReadOnlyList<PaymentStatusChangeDto> History);