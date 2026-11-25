namespace Payments.Domain.Common;

/// <summary>
/// Marker for something important that happened in the domain (past tense).
/// </summary>
public interface IDomainEvent
{
    DateTime OccurredAtUtc { get; }
}