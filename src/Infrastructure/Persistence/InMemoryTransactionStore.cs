using System.Collections.Concurrent;
using FraudMonitor.Domain.Entities;

namespace FraudMonitor.Infrastructure.Persistence;

/// <summary>
/// Simulates NoSQL (DynamoDB) and Cache (Redis) in memory with thread-safety and sub-millisecond latency.
/// </summary>
public class InMemoryTransactionStore
{
    private readonly ConcurrentDictionary<string, Transaction> _transactions = new();
    private readonly ConcurrentDictionary<string, ConcurrentBag<Transaction>> _customerHistories = new();

    public Task SaveTransactionAsync(Transaction transaction)
    {
        _transactions[transaction.TransactionId] = transaction;

        if (!string.IsNullOrWhiteSpace(transaction.ClientId))
        {
            var customerBag = _customerHistories.GetOrAdd(transaction.ClientId, _ => new ConcurrentBag<Transaction>());
            customerBag.Add(transaction);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Transaction>> GetAllTransactionsAsync()
    {
        IReadOnlyList<Transaction> list = _transactions.Values.OrderByDescending(t => t.Timestamp).ToList();
        return Task.FromResult(list);
    }

    public Task<Transaction?> GetTransactionByIdAsync(string transactionId)
    {
        _transactions.TryGetValue(transactionId, out var transaction);
        return Task.FromResult(transaction);
    }

    public Task<IReadOnlyList<Transaction>> GetRecentTransactionsByClientAsync(string clientId, TimeSpan window)
    {
        if (!_customerHistories.TryGetValue(clientId, out var bag))
        {
            return Task.FromResult<IReadOnlyList<Transaction>>(Array.Empty<Transaction>());
        }

        var cutoffTime = DateTime.UtcNow.Subtract(window);
        IReadOnlyList<Transaction> recent = bag
            .Where(t => t.Timestamp >= cutoffTime)
            .OrderByDescending(t => t.Timestamp)
            .ToList();

        return Task.FromResult(recent);
    }

    public Task<Transaction?> GetLastTransactionByClientAsync(string clientId)
    {
        if (!_customerHistories.TryGetValue(clientId, out var bag))
        {
            return Task.FromResult<Transaction?>(null);
        }

        var last = bag.OrderByDescending(t => t.Timestamp).FirstOrDefault();
        return Task.FromResult(last);
    }
}
