using FraudMonitor.Domain.Entities;
using FraudMonitor.Domain.Enums;
using FraudMonitor.Infrastructure.Persistence;
using FraudMonitor.Infrastructure.Repositories;
using Xunit;

namespace FraudMonitor.Tests.Infrastructure;

public class TransactionReadRepositoryTests
{
    private readonly InMemoryTransactionStore _store;
    private readonly TransactionReadRepository _repository;

    public TransactionReadRepositoryTests()
    {
        _store = new InMemoryTransactionStore();
        _repository = new TransactionReadRepository(_store);
    }

    private async Task PopulateSampleDataAsync()
    {
        var baseDate = new DateTime(2026, 9, 18, 10, 0, 0, DateTimeKind.Utc);

        var tx1 = new Transaction
        {
            TransactionId = "TXN-1",
            ClientId = "CLI-1",
            Amount = 100m,
            Timestamp = baseDate.AddMinutes(10),
            Status = TransactionStatus.Approved,
            RiskLevel = RiskLevel.None
        };

        var tx2 = new Transaction
        {
            TransactionId = "TXN-2",
            ClientId = "CLI-2",
            Amount = 15000m,
            Timestamp = baseDate.AddMinutes(20),
            Status = TransactionStatus.Flagged,
            RiskLevel = RiskLevel.Medium,
            RiskReason = "Atypical transaction amount"
        };

        var tx3 = new Transaction
        {
            TransactionId = "TXN-3",
            ClientId = "CLI-3",
            Amount = 80000m,
            Timestamp = baseDate.AddMinutes(30),
            Status = TransactionStatus.Flagged,
            RiskLevel = RiskLevel.High,
            RiskReason = "Extreme amount and impossible travel"
        };

        await _store.SaveTransactionAsync(tx1);
        await _store.SaveTransactionAsync(tx2);
        await _store.SaveTransactionAsync(tx3);
    }

    [Fact]
    public async Task GetFlaggedTransactionsAsync_WhenNoFilterProvided_ShouldReturnAllFlaggedOrRiskyTransactions()
    {
        // Arrange
        await PopulateSampleDataAsync();

        // Act
        var results = (await _repository.GetFlaggedTransactionsAsync(null, null, null, null)).ToList();

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Contains(results, t => t.TransactionId == "TXN-2");
        Assert.Contains(results, t => t.TransactionId == "TXN-3");
    }

    [Fact]
    public async Task GetFlaggedTransactionsAsync_FilterByStatusApproved_ShouldReturnOnlyApproved()
    {
        // Arrange
        await PopulateSampleDataAsync();

        // Act
        var results = (await _repository.GetFlaggedTransactionsAsync("Approved", null, null, null)).ToList();

        // Assert
        Assert.Single(results);
        Assert.Equal("TXN-1", results[0].TransactionId);
    }

    [Fact]
    public async Task GetFlaggedTransactionsAsync_FilterByRiskLevelHigh_ShouldReturnOnlyHighRisk()
    {
        // Arrange
        await PopulateSampleDataAsync();

        // Act
        var results = (await _repository.GetFlaggedTransactionsAsync(null, null, null, "High")).ToList();

        // Assert
        Assert.Single(results);
        Assert.Equal("TXN-3", results[0].TransactionId);
    }

    [Fact]
    public async Task GetFlaggedTransactionsAsync_FilterByDateRange_ShouldReturnTransactionsWithinRange()
    {
        // Arrange
        await PopulateSampleDataAsync();
        var start = new DateTime(2026, 9, 18, 10, 15, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 9, 18, 10, 25, 0, DateTimeKind.Utc);

        // Act
        var results = (await _repository.GetFlaggedTransactionsAsync("Flagged", start, end, null)).ToList();

        // Assert
        Assert.Single(results);
        Assert.Equal("TXN-2", results[0].TransactionId);
    }
}
