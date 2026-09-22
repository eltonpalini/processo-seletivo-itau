using FraudMonitor.Application.Models;
using FraudMonitor.Domain.Entities;

namespace FraudMonitor.Application.Interfaces.Strategy;

public interface IFraudRuleStrategy
{
    // The rule name for logging and observability
    string RuleName { get; }

    // Evaluates a transaction and returns the rule outcome
    Task<RuleEvaluationResult> EvaluateAsync(Transaction transaction);
}
