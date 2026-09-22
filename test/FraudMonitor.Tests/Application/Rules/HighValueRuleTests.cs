using FraudMonitor.Application.Rules;
using FraudMonitor.Domain.Entities;
using FraudMonitor.Domain.Enums;
using Xunit;

namespace FraudMonitor.Tests.Application.Rules;

public class HighValueRuleTests
{
    private readonly HighValueRule _rule;

    public HighValueRuleTests()
    {
        _rule = new HighValueRule(mediumThreshold: 10000m, highThreshold: 50000m);
    }

    [Fact]
    public async Task EvaluateAsync_WhenAmountIsBelowThreshold_ShouldReturnNotTriggered()
    {
        // Arrange
        var transaction = new Transaction
        {
            TransactionId = "TXN-001",
            Amount = 5000m
        };

        // Act
        var result = await _rule.EvaluateAsync(transaction);

        // Assert
        Assert.False(result.IsTriggered);
        Assert.Equal(RiskLevel.None, result.RiskLevel);
    }

    [Fact]
    public async Task EvaluateAsync_WhenAmountIsAboveMediumThreshold_ShouldReturnMediumRisk()
    {
        // Arrange
        var transaction = new Transaction
        {
            TransactionId = "TXN-002",
            Amount = 15000m
        };

        // Act
        var result = await _rule.EvaluateAsync(transaction);

        // Assert
        Assert.True(result.IsTriggered);
        Assert.Equal(RiskLevel.Medium, result.RiskLevel);
        Assert.Contains("Atypical transaction amount", result.Reason);
    }

    [Fact]
    public async Task EvaluateAsync_WhenAmountIsAboveHighThreshold_ShouldReturnHighRisk()
    {
        // Arrange
        var transaction = new Transaction
        {
            TransactionId = "TXN-003",
            Amount = 60000m
        };

        // Act
        var result = await _rule.EvaluateAsync(transaction);

        // Assert
        Assert.True(result.IsTriggered);
        Assert.Equal(RiskLevel.High, result.RiskLevel);
        Assert.Contains("Extremely atypical transaction amount", result.Reason);
    }
}
