namespace Payments.Infrastructure.BackgroundJobs;

public sealed class BackgroundJobOptions
{
    public const string SectionName = "BackgroundJobs";

    public int RetryIntervalMinutes { get; set; } = 1;
    public int BatchSize { get; set; } = 100;
}