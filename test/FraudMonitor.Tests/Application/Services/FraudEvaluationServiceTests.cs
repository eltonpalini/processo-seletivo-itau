using FraudMonitor.Application.Interfaces.Strategy;
using FraudMonitor.Application.Models;
using FraudMonitor.Application.Services;
using FraudMonitor.Domain.Entities;
using FraudMonitor.Domain.Enums;
using Moq;
using Xunit;

namespace FraudMonitor.Tests.Application.Services;

public class FraudEvaluationServiceTests
{
    [Fact]
    public async Task EvaluateTransactionAsync_WithMultipleRules_ShouldReturnHighestRiskAndFlagTransaction()
    {
        // Arrange
        var mockRuleLow = new Mock<IFraudRuleStrategy>();
        mockRuleLow.Setup(r => r.RuleName).Returns("RuleLow");
        mockRuleLow.Setup(r => r.EvaluateAsync(It.IsAny<Transaction>()))
            .ReturnsAsync(RuleEvaluationResult.Triggered("RuleLow", RiskLevel.Low, "Low risk reason"));

        var mockRuleMedium = new Mock<IFraudRuleStrategy>();
        mockRuleMedium.Setup(r => r.RuleName).Returns("RuleMedium");
        mockRuleMedium.Setup(r => r.EvaluateAsync(It.IsAny<Transaction>()))
            .ReturnsAsync(RuleEvaluationResult.Triggered("RuleMedium", RiskLevel.Medium, "Medium risk reason"));

        var rules = new List<IFraudRuleStrategy> { mockRuleLow.Object, mockRuleMedium.Object };
        var service = new FraudEvaluationService(rules);
        var transaction = new Transaction();

        // Act
        var result = await service.EvaluateTransactionAsync(transaction);

        // Assert
        Assert.Equal(RiskLevel.Medium, result);
        Assert.Equal(RiskLevel.Medium, transaction.RiskLevel);
        Assert.Equal(TransactionStatus.Flagged, transaction.Status);
        Assert.Equal(2, transaction.TriggeredRules.Count);
        Assert.Contains("[RuleLow]: Low risk reason", transaction.RiskReason);
        Assert.Contains("[RuleMedium]: Medium risk reason", transaction.RiskReason);
        Assert.NotNull(transaction.ProcessedAt);
    }

    [Fact]
    public async Task EvaluateTransactionAsync_WhenHighRiskFound_ShouldShortCircuit()
    {
        // Arrange
        var mockRuleHigh = new Mock<IFraudRuleStrategy>();
        mockRuleHigh.Setup(r => r.RuleName).Returns("RuleHigh");
        mockRuleHigh.Setup(r => r.EvaluateAsync(It.IsAny<Transaction>()))
            .ReturnsAsync(RuleEvaluationResult.Triggered("RuleHigh", RiskLevel.High, "High risk critical"));

        var mockRuleNotExecuted = new Mock<IFraudRuleStrategy>();
        mockRuleNotExecuted.Setup(r => r.RuleName).Returns("RuleNotExecuted");
        mockRuleNotExecuted.Setup(r => r.EvaluateAsync(It.IsAny<Transaction>()))
            .ReturnsAsync(RuleEvaluationResult.Triggered("RuleNotExecuted", RiskLevel.Medium, "Should not run"));

        var rules = new List<IFraudRuleStrategy> { mockRuleHigh.Object, mockRuleNotExecuted.Object };
        var service = new FraudEvaluationService(rules);
        var transaction = new Transaction();

        // Act
        var result = await service.EvaluateTransactionAsync(transaction);

        // Assert
        Assert.Equal(RiskLevel.High, result);
        Assert.Equal(TransactionStatus.Flagged, transaction.Status);
        mockRuleNotExecuted.Verify(r => r.EvaluateAsync(It.IsAny<Transaction>()), Times.Never);
    }

    [Fact]
    public async Task EvaluateTransactionAsync_WhenNoRulesTriggered_ShouldApproveTransaction()
    {
        // Arrange
        var mockRule = new Mock<IFraudRuleStrategy>();
        mockRule.Setup(r => r.RuleName).Returns("CleanRule");
        mockRule.Setup(r => r.EvaluateAsync(It.IsAny<Transaction>()))
            .ReturnsAsync(RuleEvaluationResult.NotTriggered("CleanRule"));

        var rules = new List<IFraudRuleStrategy> { mockRule.Object };
        var service = new FraudEvaluationService(rules);
        var transaction = new Transaction();

        // Act
        var result = await service.EvaluateTransactionAsync(transaction);

        // Assert
        Assert.Equal(RiskLevel.None, result);
        Assert.Equal(RiskLevel.None, transaction.RiskLevel);
        Assert.Equal(TransactionStatus.Approved, transaction.Status);
        Assert.Empty(transaction.TriggeredRules);
        Assert.Null(transaction.RiskReason);
    }
}
