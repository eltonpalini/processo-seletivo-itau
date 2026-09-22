using FraudMonitor.Domain.Entities;

namespace FraudMonitor.Application.Interfaces.Provider;

public interface ICustomerHistoryProvider
{
    /// <summary>
    /// Retorna as transações recentes do cliente dentro de uma janela de tempo específica.
    /// </summary>
    Task<IReadOnlyList<Transaction>> GetRecentTransactionsAsync(string clientId, TimeSpan window);

    /// <summary>
    /// Retorna a última transação registrada do cliente, se existir.
    /// </summary>
    Task<Transaction?> GetLastTransactionAsync(string clientId);
}
