using FraudMonitor.Application.Interfaces.Provider;
using FraudMonitor.Domain.Entities;
using FraudMonitor.Infrastructure.Persistence;

namespace FraudMonitor.Infrastructure.Services;

public class CustomerHistoryProvider : ICustomerHistoryProvider
{
    private readonly InMemoryTransactionStore _store;

    public CustomerHistoryProvider(InMemoryTransactionStore store)
    {
        _store = store;
    }

    public Task<IReadOnlyList<Transaction>> GetRecentTransactionsAsync(string clientId, TimeSpan window)
    {
        return _store.GetRecentTransactionsByClientAsync(clientId, window);
    }

    public Task<Transaction?> GetLastTransactionAsync(string clientId)
    {
        return _store.GetLastTransactionByClientAsync(clientId);
    }
}
