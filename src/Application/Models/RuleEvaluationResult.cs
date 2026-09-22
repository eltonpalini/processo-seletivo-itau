using FraudMonitor.Domain.Enums;

namespace FraudMonitor.Application.Models;

public record RuleEvaluationResult(
    RiskLevel RiskLevel,
    bool IsTriggered,
    string RuleName,
    string? Reason = null
)
{
    public static RuleEvaluationResult NotTriggered(string ruleName) =>
        new(RiskLevel.None, false, ruleName);

    public static RuleEvaluationResult Triggered(string ruleName, RiskLevel riskLevel, string reason) =>
        new(riskLevel, true, ruleName, reason);
}
