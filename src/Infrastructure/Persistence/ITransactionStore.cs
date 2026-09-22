using FraudMonitor.Domain.Entities;

namespace FraudMonitor.Infrastructure.Persistence;

public interface ITransactionStore
{
    Task SaveTransactionAsync(Transaction transaction);
    Task<IReadOnlyList<Transaction>> GetAllTransactionsAsync();
    Task<Transaction?> GetTransactionByIdAsync(string transactionId);
    Task<IReadOnlyList<Transaction>> GetRecentTransactionsByClientAsync(string clientId, TimeSpan window);
    Task<Transaction?> GetLastTransactionByClientAsync(string clientId);
}
