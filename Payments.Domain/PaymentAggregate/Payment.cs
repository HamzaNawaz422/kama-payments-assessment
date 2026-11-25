using Payments.Domain.Common;
using Payments.Domain.ValueObjects;

namespace Payments.Domain.PaymentAggregate;

/// <summary>
/// Aggregate root. All state changes go through its methods, which enforce the business rules
/// and record an audit entry for every status transition.
/// </summary>
public sealed class Payment : AggregateRoot
{
    public const int MaxAutomaticAttempts = 3;
    public const string SystemActor = "system";

    private readonly List<PaymentStatusChange> _statusHistory = new();

    public Guid UserId { get; private set; }
    public Guid ProviderId { get; private set; }                 // other aggregate referenced by ID only
    public Money Amount { get; private set; } = default!;         // value object
    public PaymentStatus Status { get; private set; }
    public string IdempotencyKey { get; private set; } = default!;
    public string RequestHash { get; private set; } = default!;   // detects "same key, different body"
    public int AttemptCount { get; private set; }
    public string? ProviderReference { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public IReadOnlyCollection<PaymentStatusChange> StatusHistory => _statusHistory.AsReadOnly();

    public bool CanBeAutoRetried =>
        Status == PaymentStatus.Failed && AttemptCount < MaxAutomaticAttempts;

    private Payment() { } // for EF Core

    // ---------- Factory ----------

    public static Payment Create(
        Guid userId, Guid providerId, Money amount, string idempotencyKey, string requestHash)
    {
        if (userId == Guid.Empty) throw new DomainException("User is required.");
        if (providerId == Guid.Empty) throw new DomainException("Provider is required.");
        if (amount is null) throw new DomainException("Amount is required.");
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            throw new DomainException("Idempotency key is required.");
        if (idempotencyKey.Length > 100)
            throw new DomainException("Idempotency key cannot exceed 100 characters.");

        var now = DateTime.UtcNow;

        var payment = new Payment
        {
            UserId = userId,
            ProviderId = providerId,
            Amount = amount,
            Status = PaymentStatus.Pending,              // always starts as Pending
            IdempotencyKey = idempotencyKey.Trim(),
            RequestHash = requestHash,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        payment.RecordStatusChange(null, PaymentStatus.Pending, userId.ToString(), "Payment created");
        payment.AddDomainEvent(new PaymentCreated(payment.Id, userId, now));

        return payment;
    }

    // ---------- Behaviour (business rules live here) ----------

    public void StartProcessing()
    {
        if (Status is not (PaymentStatus.Pending or PaymentStatus.Failed))
            throw new DomainException($"Cannot start processing a payment in status '{Status}'.");

        if (Status == PaymentStatus.Failed && !CanBeAutoRetried)
            throw new DomainException("Maximum number of processing attempts reached.");

        AttemptCount++;
        ChangeStatus(PaymentStatus.Processing, SystemActor, $"Processing attempt {AttemptCount}");
    }

    public void MarkCompleted(string providerReference)
    {
        if (Status != PaymentStatus.Processing)
            throw new DomainException("Only a payment in processing can be completed.");
        if (string.IsNullOrWhiteSpace(providerReference))
            throw new DomainException("Provider reference is required.");

        ProviderReference = providerReference;
        FailureReason = null;
        ChangeStatus(PaymentStatus.Completed, SystemActor, "Confirmed by provider");
        AddDomainEvent(new PaymentCompleted(Id, providerReference, DateTime.UtcNow));
    }

    public void MarkFailed(string reason)
    {
        if (Status != PaymentStatus.Processing)
            throw new DomainException("Only a payment in processing can fail.");

        FailureReason = string.IsNullOrWhiteSpace(reason) ? "Unknown error" : reason;
        ChangeStatus(PaymentStatus.Failed, SystemActor, FailureReason);
        AddDomainEvent(new PaymentFailed(Id, AttemptCount, FailureReason, DateTime.UtcNow));
    }

    /// <summary>Used on startup: processing was interrupted (e.g. app restart) → put back in the queue.</summary>
    public void RequeueAfterInterruption()
    {
        if (Status != PaymentStatus.Processing)
            throw new DomainException("Only a payment in processing can be re-queued.");

        ChangeStatus(PaymentStatus.Pending, SystemActor, "Re-queued after interrupted processing");
    }

    /// <summary>Admin-only manual retry of a failed payment. Resets the automatic attempt counter.</summary>
    public void RetryManually(Guid adminUserId)
    {
        if (Status != PaymentStatus.Failed)
            throw new DomainException("Only failed payments can be retried.");

        AttemptCount = 0;
        FailureReason = null;
        ChangeStatus(PaymentStatus.Pending, adminUserId.ToString(), "Manual retry by admin");
    }

    // ---------- Helpers ----------

    private void ChangeStatus(PaymentStatus newStatus, string changedBy, string? note)
    {
        var oldStatus = Status;
        Status = newStatus;
        UpdatedAtUtc = DateTime.UtcNow;
        RecordStatusChange(oldStatus, newStatus, changedBy, note);
    }

    private void RecordStatusChange(PaymentStatus? from, PaymentStatus to, string changedBy, string? note)
        => _statusHistory.Add(new PaymentStatusChange(from, to, changedBy, note));
}