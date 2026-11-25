namespace Payments.Domain.Common;

/// <summary>
/// Base for all entities: identity + equality by Id.
/// </summary>
public abstract class Entity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();

    public override bool Equals(object? obj)
        => obj is Entity other && GetType() == other.GetType() && Id == other.Id;

    public override int GetHashCode() => Id.GetHashCode();
}