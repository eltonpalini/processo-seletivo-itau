using FraudMonitor.Domain.Enums;
using FraudMonitor.Domain.ValueObjects;

namespace FraudMonitor.Domain.Entities;

public class Transaction
{
    public string TransactionId { get; set; } = Guid.NewGuid().ToString();
    public string ClientId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string DeviceId { get; set; } = string.Empty;
    public Location? Location { get; set; }

    // Analysis results populated during/after evaluation
    public TransactionStatus Status { get; set; } = TransactionStatus.Pending;
    public RiskLevel RiskLevel { get; set; } = RiskLevel.None;
    public List<string> TriggeredRules { get; set; } = new();
    public string? RiskReason { get; set; }
    public DateTime? ProcessedAt { get; set; }
}
