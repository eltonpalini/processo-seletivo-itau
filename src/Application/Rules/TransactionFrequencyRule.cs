using FraudMonitor.Application.Interfaces.Provider;
using FraudMonitor.Application.Interfaces.Strategy;
using FraudMonitor.Application.Models;
using FraudMonitor.Domain.Entities;
using FraudMonitor.Domain.Enums;

namespace FraudMonitor.Application.Rules;

public class TransactionFrequencyRule : IFraudRuleStrategy
{
    public string RuleName => "UnusualFrequency";

    private readonly ICustomerHistoryProvider _historyProvider;
    public TimeSpan TimeWindow { get; }
    public int MediumCountThreshold { get; }
    public int HighCountThreshold { get; }

    // Primary constructor used by DI container
    public TransactionFrequencyRule(ICustomerHistoryProvider historyProvider)
        : this(historyProvider, TimeSpan.FromMinutes(1), 3, 5)
    {
    }

    public TransactionFrequencyRule(
        ICustomerHistoryProvider historyProvider,
        TimeSpan timeWindow,
        int mediumCountThreshold,
        int highCountThreshold)
    {
        _historyProvider = historyProvider;
        TimeWindow = timeWindow;
        MediumCountThreshold = mediumCountThreshold;
        HighCountThreshold = highCountThreshold;
    }

    public async Task<RuleEvaluationResult> EvaluateAsync(Transaction transaction)
    {
        if (string.IsNullOrWhiteSpace(transaction.ClientId))
        {
            return RuleEvaluationResult.NotTriggered(RuleName);
        }

        var recentTransactions = await _historyProvider.GetRecentTransactionsAsync(transaction.ClientId, TimeWindow);
        var recentCount = recentTransactions.Count;

        if (recentCount >= HighCountThreshold)
        {
            return RuleEvaluationResult.Triggered(
                RuleName,
                RiskLevel.High,
                $"Critical transaction frequency: {recentCount} transactions registered within {TimeWindow.TotalSeconds:N0}s."
            );
        }

        if (recentCount >= MediumCountThreshold)
        {
            return RuleEvaluationResult.Triggered(
                RuleName,
                RiskLevel.Medium,
                $"Unusual transaction frequency: {recentCount} transactions registered within {TimeWindow.TotalSeconds:N0}s."
            );
        }

        return RuleEvaluationResult.NotTriggered(RuleName);
    }
}
