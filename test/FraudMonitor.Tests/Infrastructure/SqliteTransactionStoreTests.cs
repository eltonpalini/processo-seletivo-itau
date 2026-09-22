using FraudMonitor.Domain.Entities;
using FraudMonitor.Domain.Enums;
using FraudMonitor.Domain.ValueObjects;
using FraudMonitor.Infrastructure.Persistence;
using Xunit;

namespace FraudMonitor.Tests.Infrastructure;

public class SqliteTransactionStoreTests : IDisposable
{
    private readonly string _tempDbPath;
    private readonly SqliteTransactionStore _store;

    public SqliteTransactionStoreTests()
    {
        _tempDbPath = Path.Combine(Path.GetTempPath(), $"fraud_test_{Guid.NewGuid():N}.db");
        _store = new SqliteTransactionStore(_tempDbPath);
    }

    public void Dispose()
    {
        try
        {
            if (File.Exists(_tempDbPath))
                File.Delete(_tempDbPath);
        }
        catch { }
    }

    [Fact]
    public async Task SaveAndGetTransactionByIdAsync_ShouldPersistAndRetrieveCorrectly()
    {
        // Arrange
        var tx = new Transaction
        {
            TransactionId = "TXN-TEST-1",
            ClientId = "CLI-TEST-1",
            Amount = 1500.50m,
            Timestamp = DateTime.UtcNow,
            DeviceId = "DEV-99",
            Location = new Location(-23.5505, -46.6333, "São Paulo", "SP", "BR"),
            Status = TransactionStatus.Flagged,
            RiskLevel = RiskLevel.High,
            TriggeredRules = new List<string> { "HighValueTransaction", "DivergentGeolocation" },
            RiskReason = "Test risk reason",
            ProcessedAt = DateTime.UtcNow
        };

        // Act
        await _store.SaveTransactionAsync(tx);
        var retrieved = await _store.GetTransactionByIdAsync("TXN-TEST-1");

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal("TXN-TEST-1", retrieved.TransactionId);
        Assert.Equal("CLI-TEST-1", retrieved.ClientId);
        Assert.Equal(1500.50m, retrieved.Amount);
        Assert.Equal(TransactionStatus.Flagged, retrieved.Status);
        Assert.Equal(RiskLevel.High, retrieved.RiskLevel);
        Assert.Equal("São Paulo", retrieved.Location?.City);
        Assert.Equal(2, retrieved.TriggeredRules.Count);
        Assert.Contains("HighValueTransaction", retrieved.TriggeredRules);
    }

    [Fact]
    public async Task GetRecentTransactionsByClientAsync_ShouldRespectTimeWindow()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var txOld = new Transaction
        {
            TransactionId = "TXN-OLD",
            ClientId = "CLI-WINDOW",
            Amount = 50m,
            Timestamp = now.AddMinutes(-10)
        };
        var txRecent = new Transaction
        {
            TransactionId = "TXN-RECENT",
            ClientId = "CLI-WINDOW",
            Amount = 100m,
            Timestamp = now.AddSeconds(-30)
        };

        await _store.SaveTransactionAsync(txOld);
        await _store.SaveTransactionAsync(txRecent);

        // Act (window of 2 minutes)
        var recent = await _store.GetRecentTransactionsByClientAsync("CLI-WINDOW", TimeSpan.FromMinutes(2));

        // Assert
        Assert.Single(recent);
        Assert.Equal("TXN-RECENT", recent[0].TransactionId);
    }

    [Fact]
    public async Task GetLastTransactionByClientAsync_ShouldReturnMostRecent()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var tx1 = new Transaction
        {
            TransactionId = "TXN-L1",
            ClientId = "CLI-LAST",
            Amount = 100m,
            Timestamp = now.AddMinutes(-5)
        };
        var tx2 = new Transaction
        {
            TransactionId = "TXN-L2",
            ClientId = "CLI-LAST",
            Amount = 200m,
            Timestamp = now.AddMinutes(-1)
        };

        await _store.SaveTransactionAsync(tx1);
        await _store.SaveTransactionAsync(tx2);

        // Act
        var last = await _store.GetLastTransactionByClientAsync("CLI-LAST");

        // Assert
        Assert.NotNull(last);
        Assert.Equal("TXN-L2", last.TransactionId);
        Assert.Equal(200m, last.Amount);
    }
}
