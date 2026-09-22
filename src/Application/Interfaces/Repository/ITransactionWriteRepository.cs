using FraudMonitor.Domain.Entities;
using FraudMonitor.Domain.Enums;

namespace FraudMonitor.Application.Interfaces.Repository;

public interface ITransactionWriteRepository
{
    Task SaveTransactionRiskAsync(Transaction transaction, RiskLevel riskLevel);
}
