using Payments.Domain.Common;

namespace Payments.Domain.PaymentAggregate;

public sealed record PaymentCreated(Guid PaymentId, Guid UserId, DateTime OccurredAtUtc) : IDomainEvent;

public sealed record PaymentCompleted(Guid PaymentId, string ProviderReference, DateTime OccurredAtUtc) : IDomainEvent;

public sealed record PaymentFailed(Guid PaymentId, int AttemptCount, string Reason, DateTime OccurredAtUtc) : IDomainEvent;