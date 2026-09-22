using FraudMonitor.Application.Interfaces.Repository;
using FraudMonitor.Domain.Entities;
using FraudMonitor.Domain.Enums;
using FraudMonitor.Infrastructure.Persistence;

namespace FraudMonitor.Infrastructure.Repositories;

public class TransactionReadRepository : ITransactionReadRepository
{
    private readonly ITransactionStore _store;

    public TransactionReadRepository(ITransactionStore store)
    {
        _store = store;
    }

    public async Task<IEnumerable<Transaction>> GetFlaggedTransactionsAsync(
        string? status,
        DateTime? startDate,
        DateTime? endDate,
        string? riskLevel)
    {
        var allTransactions = await _store.GetAllTransactionsAsync();
        var query = allTransactions.AsEnumerable();

        // 1. Status Filter
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (Enum.TryParse<TransactionStatus>(status, ignoreCase: true, out var parsedStatus))
            {
                query = query.Where(t => t.Status == parsedStatus);
            }
            else
            {
                query = query.Where(t => t.Status.ToString().Equals(status, StringComparison.OrdinalIgnoreCase));
            }
        }
        else
        {
            query = query.Where(t => t.Status == TransactionStatus.Flagged || t.RiskLevel != RiskLevel.None);
        }

        // 2. Date Range Filter
        if (startDate.HasValue)
        {
            query = query.Where(t => t.Timestamp >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(t => t.Timestamp <= endDate.Value);
        }

        // 3. Risk Level Filter
        if (!string.IsNullOrWhiteSpace(riskLevel))
        {
            if (Enum.TryParse<RiskLevel>(riskLevel, ignoreCase: true, out var parsedRisk))
            {
                query = query.Where(t => t.RiskLevel == parsedRisk);
            }
            else
            {
                query = query.Where(t => t.RiskLevel.ToString().Equals(riskLevel, StringComparison.OrdinalIgnoreCase));
            }
        }

        return query.ToList();
    }
}
