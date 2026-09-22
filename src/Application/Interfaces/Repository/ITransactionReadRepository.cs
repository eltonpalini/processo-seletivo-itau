using FraudMonitor.Domain.Entities;

namespace FraudMonitor.Application.Interfaces.Repository;

public interface ITransactionReadRepository
{
    Task<IEnumerable<Transaction>> GetFlaggedTransactionsAsync(string? status, DateTime? startDate, DateTime? endDate, string? riskLevel);
}
