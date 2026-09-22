using FraudMonitor.Application.Interfaces.Provider;
using FraudMonitor.Application.Rules;
using FraudMonitor.Domain.Entities;
using FraudMonitor.Domain.Enums;
using FraudMonitor.Domain.ValueObjects;
using Moq;
using Xunit;

namespace FraudMonitor.Tests.Application.Rules;

public class GeoVelocityRuleTests
{
    private readonly Mock<ICustomerHistoryProvider> _historyMock;
    private readonly GeoVelocityRule _rule;

    public GeoVelocityRuleTests()
    {
        _historyMock = new Mock<ICustomerHistoryProvider>();
        _rule = new GeoVelocityRule(_historyMock.Object, maxFeasibleSpeedKmH: 800.0, suspiciousSpeedKmH: 250.0);
    }

    [Fact]
    public async Task EvaluateAsync_WhenLocationsAreClose_ShouldReturnNotTriggered()
    {
        // Arrange
        var clientId = "CLI-GEO-001";
        var now = DateTime.UtcNow;

        var lastTransaction = new Transaction
        {
            ClientId = clientId,
            Timestamp = now.AddMinutes(-5),
            Location = new Location(-23.5505, -46.6333, "São Paulo", "SP") // Centro de SP
        };

        var currentTransaction = new Transaction
        {
            ClientId = clientId,
            Timestamp = now,
            Location = new Location(-23.5615, -46.6559, "São Paulo", "SP") // Paulista (~3km de distância)
        };

        _historyMock.Setup(h => h.GetLastTransactionAsync(clientId)).ReturnsAsync(lastTransaction);

        // Act
        var result = await _rule.EvaluateAsync(currentTransaction);

        // Assert
        Assert.False(result.IsTriggered);
        Assert.Equal(RiskLevel.None, result.RiskLevel);
    }

    [Fact]
    public async Task EvaluateAsync_WhenTravelIsPhysicallyImpossible_ShouldReturnHighRisk()
    {
        // Arrange
        var clientId = "CLI-GEO-002";
        var now = DateTime.UtcNow;

        // Last transaction in São Paulo 10 minutes ago
        var lastTransaction = new Transaction
        {
            ClientId = clientId,
            Timestamp = now.AddMinutes(-10),
            Location = new Location(-23.5505, -46.6333, "São Paulo", "SP")
        };

        // Current transaction in New York (~7680 km in 10 minutes)
        var currentTransaction = new Transaction
        {
            ClientId = clientId,
            Timestamp = now,
            Location = new Location(40.7128, -74.0060, "New York", "NY", "US")
        };

        _historyMock.Setup(h => h.GetLastTransactionAsync(clientId)).ReturnsAsync(lastTransaction);

        // Act
        var result = await _rule.EvaluateAsync(currentTransaction);

        // Assert
        Assert.True(result.IsTriggered);
        Assert.Equal(RiskLevel.High, result.RiskLevel);
        Assert.Contains("Impossible travel", result.Reason);
    }

    [Fact]
    public async Task EvaluateAsync_WhenNoPreviousLocationAvailable_ShouldReturnNotTriggered()
    {
        // Arrange
        var clientId = "CLI-GEO-003";
        var currentTransaction = new Transaction
        {
            ClientId = clientId,
            Timestamp = DateTime.UtcNow,
            Location = new Location(-23.5505, -46.6333, "São Paulo", "SP")
        };

        _historyMock.Setup(h => h.GetLastTransactionAsync(clientId)).ReturnsAsync((Transaction?)null);

        // Act
        var result = await _rule.EvaluateAsync(currentTransaction);

        // Assert
        Assert.False(result.IsTriggered);
    }
}
