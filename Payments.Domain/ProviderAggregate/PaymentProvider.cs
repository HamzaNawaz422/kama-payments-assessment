using Payments.Domain.Common;

namespace Payments.Domain.ProviderAggregate;

/// <summary>
/// Aggregate root for a payment provider configured by admins.
/// </summary>
public sealed class PaymentProvider : AggregateRoot
{
    public string Name { get; private set; } = default!;
    public string Code { get; private set; } = default!;
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private PaymentProvider() { } // for EF Core

    public static PaymentProvider Create(string name, string code)
    {
        ValidateName(name);
        if (string.IsNullOrWhiteSpace(code) || code.Length > 50)
            throw new DomainException("Provider code is required (max 50 characters).");

        return new PaymentProvider
        {
            Name = name.Trim(),
            Code = code.Trim().ToUpperInvariant(),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public void Rename(string name)
    {
        ValidateName(name);
        Name = name.Trim();
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 100)
            throw new DomainException("Provider name is required (max 100 characters).");
    }
}