namespace Payments.Infrastructure.ExternalServices;

public sealed class PaymentProviderOptions
{
    public const string SectionName = "PaymentProvider";

    public string BaseUrl { get; set; } = default!;
    public int AttemptTimeoutSeconds { get; set; } = 3;
    public int TotalTimeoutSeconds { get; set; } = 20;
    public int MaxRetryAttempts { get; set; } = 3;
}