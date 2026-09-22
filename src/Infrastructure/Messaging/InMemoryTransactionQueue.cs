using System.Threading.Channels;
using FraudMonitor.Application.Interfaces.Queue;
using FraudMonitor.Domain.Entities;

namespace FraudMonitor.Infrastructure.Messaging;

public class InMemoryTransactionQueue : ITransactionQueue
{
    private readonly Channel<Transaction> _channel;

    public InMemoryTransactionQueue(int capacity = 50000)
    {
        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleWriter = false,
            SingleReader = false
        };

        _channel = Channel.CreateBounded<Transaction>(options);
    }

    public ValueTask EnqueueAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        return _channel.Writer.WriteAsync(transaction, cancellationToken);
    }

    public IAsyncEnumerable<Transaction> ReadAllAsync(CancellationToken cancellationToken = default)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }
}
