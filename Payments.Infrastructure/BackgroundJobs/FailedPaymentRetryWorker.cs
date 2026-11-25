using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Payments.Application.Abstractions;
using Payments.Domain.PaymentAggregate;

namespace Payments.Infrastructure.BackgroundJobs;

public sealed class FailedPaymentRetryWorker(
    IPaymentQueue queue,
    IServiceScopeFactory scopeFactory,
    BackgroundJobOptions options,
    ILogger<FailedPaymentRetryWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(options.RetryIntervalMinutes));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var payments = scope.ServiceProvider.GetRequiredService<IPaymentRepository>();

                var failedIds = await payments.GetRetryableFailedIdsAsync(
                    Payment.MaxAutomaticAttempts, options.BatchSize, stoppingToken);

                foreach (var id in failedIds)
                    await queue.EnqueueAsync(id, stoppingToken);

                if (failedIds.Count > 0)
                    logger.LogInformation("Retry job re-queued {Count} failed payments", failedIds.Count);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(ex, "Failed payment retry job error");
            }
        }
    }
}