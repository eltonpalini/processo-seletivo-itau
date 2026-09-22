using FraudMonitor.Application.Interfaces.Strategy;
using FraudMonitor.Application.Models;
using FraudMonitor.Domain.Entities;
using FraudMonitor.Domain.Enums;

namespace FraudMonitor.Application.Rules;

public class HighValueRule : IFraudRuleStrategy
{
    public string RuleName => "HighValueTransaction";

    public decimal MediumThreshold { get; }
    public decimal HighThreshold { get; }

    // Default constructor used by dynamic DI reflection
    public HighValueRule() : this(10000m, 50000m)
    {
    }

    public HighValueRule(decimal mediumThreshold, decimal highThreshold)
    {
        MediumThreshold = mediumThreshold;
        HighThreshold = highThreshold;
    }

    public Task<RuleEvaluationResult> EvaluateAsync(Transaction transaction)
    {
        if (transaction.Amount >= HighThreshold)
        {
            return Task.FromResult(RuleEvaluationResult.Triggered(
                RuleName,
                RiskLevel.High,
                $"Extremely atypical transaction amount of ${transaction.Amount:N2}, exceeding the critical threshold of ${HighThreshold:N2}."
            ));
        }

        if (transaction.Amount >= MediumThreshold)
        {
            return Task.FromResult(RuleEvaluationResult.Triggered(
                RuleName,
                RiskLevel.Medium,
                $"Atypical transaction amount of ${transaction.Amount:N2}, exceeding the attention threshold of ${MediumThreshold:N2}."
            ));
        }

        return Task.FromResult(RuleEvaluationResult.NotTriggered(RuleName));
    }
}
