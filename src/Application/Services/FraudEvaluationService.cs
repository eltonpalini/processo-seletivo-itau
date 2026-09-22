using FraudMonitor.Application.Interfaces.Strategy;
using FraudMonitor.Domain.Entities;
using FraudMonitor.Domain.Enums;

namespace FraudMonitor.Application.Services;

public class FraudEvaluationService
{
    private readonly IEnumerable<IFraudRuleStrategy> _rules;

    // The rules are injected by dependency injection (Open-Closed Principle - OCP).
    public FraudEvaluationService(IEnumerable<IFraudRuleStrategy> rules)
    {
        _rules = rules;
    }

    public async Task<RiskLevel> EvaluateTransactionAsync(Transaction transaction)
    {
        var highestRisk = RiskLevel.None;
        var triggeredRules = new List<string>();
        var reasons = new List<string>();

        foreach (var rule in _rules)
        {
            var result = await rule.EvaluateAsync(transaction);

            if (result.IsTriggered)
            {
                triggeredRules.Add(result.RuleName);
                if (!string.IsNullOrWhiteSpace(result.Reason))
                {
                    reasons.Add($"[{result.RuleName}]: {result.Reason}");
                }

                if (result.RiskLevel > highestRisk)
                {
                    highestRisk = result.RiskLevel;
                }

                // Low-latency optimization (< 1s):
                // If High risk is identified, short-circuit further evaluations
                if (highestRisk == RiskLevel.High)
                {
                    break;
                }
            }
        }

        transaction.RiskLevel = highestRisk;
        transaction.TriggeredRules = triggeredRules;
        transaction.RiskReason = reasons.Count > 0 ? string.Join(" | ", reasons) : null;
        transaction.Status = highestRisk switch
        {
            RiskLevel.High or RiskLevel.Medium or RiskLevel.Low => TransactionStatus.Flagged,
            _ => TransactionStatus.Approved
        };
        transaction.ProcessedAt = DateTime.UtcNow;

        return highestRisk;
    }
}
