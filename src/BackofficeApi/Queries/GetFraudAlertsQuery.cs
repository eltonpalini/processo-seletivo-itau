namespace FraudMonitor.BackofficeApi.Queries;

public class GetFraudAlertsQuery
{
    public string? Status { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? RiskLevel { get; set; }
}
