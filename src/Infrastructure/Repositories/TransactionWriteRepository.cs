using FraudMonitor.Application.Interfaces.Repository;
using FraudMonitor.Domain.Entities;
using FraudMonitor.Domain.Enums;
using FraudMonitor.Infrastructure.Persistence;

namespace FraudMonitor.Infrastructure.Repositories;

public class TransactionWriteRepository : ITransactionWriteRepository
{
    private readonly InMemoryTransactionStore _store;

    public TransactionWriteRepository(InMemoryTransactionStore store)
    {
        _store = store;
    }

    public async Task SaveTransactionRiskAsync(Transaction transaction, RiskLevel riskLevel)
    {
        transaction.RiskLevel = riskLevel;
        await _store.SaveTransactionAsync(transaction);
    }
}
