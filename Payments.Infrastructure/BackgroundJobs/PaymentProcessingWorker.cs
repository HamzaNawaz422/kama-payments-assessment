using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Payments.Application.Abstractions;
using Payments.Application.Payments;
using Payments.Domain.PaymentAggregate;

namespace Payments.Infrastructure.BackgroundJobs;

public sealed class PaymentProcessingWorker(
    IPaymentQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<PaymentProcessingWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RecoverUnfinishedPaymentsAsync(stoppingToken);

        await foreach (var paymentId in queue.DequeueAllAsync(stoppingToken))
        {
            try
            {
                // BackgroundService is a singleton, so each payment gets its own scope (and its own DbContext)
                await using var scope = scopeFactory.CreateAsyncScope();
                var processor = scope.ServiceProvider.GetRequiredService<PaymentProcessor>();
                await processor.ProcessAsync(paymentId, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // One bad payment must never stop the worker
                logger.LogError(ex, "Unexpected error while processing payment {PaymentId}", paymentId);
            }
        }
    }

    private async Task RecoverUnfinishedPaymentsAsync(CancellationToken ct)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var payments = scope.ServiceProvider.GetRequiredService<IPaymentRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            // Processing was cut off by a restart: put those payments back to Pending
            var interrupted = await payments.GetProcessingAsync(ct);
            foreach (var payment in interrupted)
                payment.RequeueAfterInterruption();

            if (interrupted.Count > 0)
            {
                await unitOfWork.SaveChangesAsync(ct);
                logger.LogWarning("Re-queued {Count} payments interrupted during processing", interrupted.Count);
            }

            // Everything still Pending goes back into the queue
            var pendingIds = await payments.GetPendingIdsAsync(1000, ct);
            foreach (var id in pendingIds)
                await queue.EnqueueAsync(id, ct);

            logger.LogInformation("Startup recovery: {Count} pending payments queued", pendingIds.Count);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            logger.LogError(ex, "Startup recovery failed");
        }
    }
}