using System.Threading.Channels;
using Payments.Application.Abstractions;

namespace Payments.Infrastructure.BackgroundJobs;

public sealed class ChannelPaymentQueue : IPaymentQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>(
        new UnboundedChannelOptions { SingleReader = true });

    public ValueTask EnqueueAsync(Guid paymentId, CancellationToken ct)
        => _channel.Writer.WriteAsync(paymentId, ct);

    public IAsyncEnumerable<Guid> DequeueAllAsync(CancellationToken ct)
        => _channel.Reader.ReadAllAsync(ct);
}