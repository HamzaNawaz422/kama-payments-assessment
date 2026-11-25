using Payments.Domain.Common;

namespace Payments.Domain.ValueObjects;

/// <summary>
/// Value object: an amount with its currency. Immutable, no identity, validated on creation.
/// Stored as decimal (never float/double).
/// </summary>
public sealed record Money
{
    private static readonly HashSet<string> SupportedCurrencies =
        new(StringComparer.OrdinalIgnoreCase) { "USD", "EUR", "GBP", "AED", "SAR" };

    public decimal Amount { get; private init; }
    public string Currency { get; private init; } = default!;

    private Money() { } // for EF Core

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Money Create(decimal amount, string currency)
    {
        if (amount <= 0)
            throw new DomainException("Amount must be greater than zero.");

        if (decimal.Round(amount, 2) != amount)
            throw new DomainException("Amount cannot have more than 2 decimal places.");

        if (string.IsNullOrWhiteSpace(currency) || !SupportedCurrencies.Contains(currency))
            throw new DomainException($"Currency '{currency}' is not supported.");

        return new Money(amount, currency.ToUpperInvariant());
    }

    public static bool IsSupportedCurrency(string currency)
        => !string.IsNullOrWhiteSpace(currency) && SupportedCurrencies.Contains(currency);

    public override string ToString() => $"{Amount:0.00} {Currency}";
}