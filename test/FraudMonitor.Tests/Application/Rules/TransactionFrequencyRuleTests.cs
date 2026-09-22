using FraudMonitor.Application.Interfaces.Provider;
using FraudMonitor.Application.Rules;
using FraudMonitor.Domain.Entities;
using FraudMonitor.Domain.Enums;
using Moq;
using Xunit;

namespace FraudMonitor.Tests.Application.Rules;

public class TransactionFrequencyRuleTests
{
    private readonly Mock<ICustomerHistoryProvider> _historyMock;
    private readonly TransactionFrequencyRule _rule;

    public TransactionFrequencyRuleTests()
    {
        _historyMock = new Mock<ICustomerHistoryProvider>();
        _rule = new TransactionFrequencyRule(_historyMock.Object, TimeSpan.FromMinutes(1), mediumCountThreshold: 3, highCountThreshold: 5);
    }

    [Fact]
    public async Task EvaluateAsync_WhenRecentTransactionsBelowMediumThreshold_ShouldReturnNotTriggered()
    {
        // Arrange
        var clientId = "CLI-001";
        var transaction = new Transaction { ClientId = clientId, Amount = 100m };

        _historyMock.Setup(h => h.GetRecentTransactionsAsync(clientId, It.IsAny<TimeSpan>()))
            .ReturnsAsync(new List<Transaction>
            {
                new() { ClientId = clientId, Amount = 50m },
                new() { ClientId = clientId, Amount = 80m }
            });

        // Act
        var result = await _rule.EvaluateAsync(transaction);

        // Assert
        Assert.False(result.IsTriggered);
        Assert.Equal(RiskLevel.None, result.RiskLevel);
    }

    [Fact]
    public async Task EvaluateAsync_WhenRecentTransactionsReachMediumThreshold_ShouldReturnMediumRisk()
    {
        // Arrange
        var clientId = "CLI-002";
        var transaction = new Transaction { ClientId = clientId, Amount = 100m };

        _historyMock.Setup(h => h.GetRecentTransactionsAsync(clientId, It.IsAny<TimeSpan>()))
            .ReturnsAsync(new List<Transaction>
            {
                new() { ClientId = clientId },
                new() { ClientId = clientId },
                new() { ClientId = clientId }
            });

        // Act
        var result = await _rule.EvaluateAsync(transaction);

        // Assert
        Assert.True(result.IsTriggered);
        Assert.Equal(RiskLevel.Medium, result.RiskLevel);
        Assert.Contains("3 transactions", result.Reason);
    }

    [Fact]
    public async Task EvaluateAsync_WhenRecentTransactionsReachHighThreshold_ShouldReturnHighRisk()
    {
        // Arrange
        var clientId = "CLI-003";
        var transaction = new Transaction { ClientId = clientId, Amount = 100m };

        _historyMock.Setup(h => h.GetRecentTransactionsAsync(clientId, It.IsAny<TimeSpan>()))
            .ReturnsAsync(new List<Transaction>
            {
                new() { ClientId = clientId },
                new() { ClientId = clientId },
                new() { ClientId = clientId },
                new() { ClientId = clientId },
                new() { ClientId = clientId }
            });

        // Act
        var result = await _rule.EvaluateAsync(transaction);

        // Assert
        Assert.True(result.IsTriggered);
        Assert.Equal(RiskLevel.High, result.RiskLevel);
        Assert.Contains("5 transactions", result.Reason);
    }
}
