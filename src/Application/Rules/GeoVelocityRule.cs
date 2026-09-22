using FraudMonitor.Application.Interfaces.Provider;
using FraudMonitor.Application.Interfaces.Strategy;
using FraudMonitor.Application.Models;
using FraudMonitor.Domain.Entities;
using FraudMonitor.Domain.Enums;

namespace FraudMonitor.Application.Rules;

public class GeoVelocityRule : IFraudRuleStrategy
{
    public string RuleName => "DivergentGeolocation";

    private readonly ICustomerHistoryProvider _historyProvider;
    public double MaxFeasibleSpeedKmH { get; }
    public double SuspiciousSpeedKmH { get; }

    // Primary constructor used by DI container
    public GeoVelocityRule(ICustomerHistoryProvider historyProvider)
        : this(historyProvider, 800.0, 250.0)
    {
    }

    public GeoVelocityRule(
        ICustomerHistoryProvider historyProvider,
        double maxFeasibleSpeedKmH,
        double suspiciousSpeedKmH)
    {
        _historyProvider = historyProvider;
        MaxFeasibleSpeedKmH = maxFeasibleSpeedKmH;
        SuspiciousSpeedKmH = suspiciousSpeedKmH;
    }

    public async Task<RuleEvaluationResult> EvaluateAsync(Transaction transaction)
    {
        if (transaction.Location == null || string.IsNullOrWhiteSpace(transaction.ClientId))
        {
            return RuleEvaluationResult.NotTriggered(RuleName);
        }

        var lastTransaction = await _historyProvider.GetLastTransactionAsync(transaction.ClientId);
        if (lastTransaction?.Location == null)
        {
            return RuleEvaluationResult.NotTriggered(RuleName);
        }

        var distanceKm = lastTransaction.Location.DistanceToInKm(transaction.Location);
        
        // If the distance is negligible (same neighborhood/city < 5km), not a divergence
        if (distanceKm < 5.0)
        {
            return RuleEvaluationResult.NotTriggered(RuleName);
        }

        var elapsedHours = Math.Abs((transaction.Timestamp - lastTransaction.Timestamp).TotalHours);

        // Prevents division by zero or virtually simultaneous transactions in different places
        if (elapsedHours < 0.001) // less than 3.6 seconds
        {
            return RuleEvaluationResult.Triggered(
                RuleName,
                RiskLevel.High,
                $"Teleportation detected: transaction performed {distanceKm:N0} km away from previous location in virtually simultaneous timeframe."
            );
        }

        var requiredSpeedKmH = distanceKm / elapsedHours;

        if (requiredSpeedKmH > MaxFeasibleSpeedKmH)
        {
            return RuleEvaluationResult.Triggered(
                RuleName,
                RiskLevel.High,
                $"Impossible travel detected: displacement of {distanceKm:N0} km in {elapsedHours * 60:N1} minutes requires a speed of {requiredSpeedKmH:N0} km/h (exceeding feasible threshold of {MaxFeasibleSpeedKmH:N0} km/h)."
            );
        }

        if (requiredSpeedKmH > SuspiciousSpeedKmH)
        {
            return RuleEvaluationResult.Triggered(
                RuleName,
                RiskLevel.Medium,
                $"Suspicious geographic displacement: displacement of {distanceKm:N0} km in {elapsedHours * 60:N1} minutes requires a speed of {requiredSpeedKmH:N0} km/h."
            );
        }

        return RuleEvaluationResult.NotTriggered(RuleName);
    }
}
