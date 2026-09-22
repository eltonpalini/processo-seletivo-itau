using FraudMonitor.Domain.Entities;

namespace FraudMonitor.Application.Interfaces.Queue;

public interface ITransactionQueue
{
    ValueTask EnqueueAsync(Transaction transaction, CancellationToken cancellationToken = default);
    IAsyncEnumerable<Transaction> ReadAllAsync(CancellationToken cancellationToken = default);
}
