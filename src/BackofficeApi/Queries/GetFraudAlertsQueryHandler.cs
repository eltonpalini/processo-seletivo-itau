using FraudMonitor.Application.Interfaces.Repository;
using FraudMonitor.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace FraudMonitor.BackofficeApi.Queries;

public class GetFraudAlertsQueryHandler
{
    private readonly ITransactionReadRepository _readRepository;
    private readonly ILogger<GetFraudAlertsQueryHandler> _logger;

    public GetFraudAlertsQueryHandler(ITransactionReadRepository readRepository, ILogger<GetFraudAlertsQueryHandler> logger)
    {
        _readRepository = readRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<Transaction>> HandleAsync(GetFraudAlertsQuery query)
    {
        _logger.LogInformation(
            "[AUDIT_QUERY] Fetching fraud alerts. Status: {Status}, RiskLevel: {RiskLevel}, StartDate: {StartDate}, EndDate: {EndDate}",
            query.Status,
            query.RiskLevel,
            query.StartDate,
            query.EndDate
        );

        var results = (await _readRepository.GetFlaggedTransactionsAsync(
            query.Status, 
            query.StartDate, 
            query.EndDate, 
            query.RiskLevel)).ToList();

        _logger.LogInformation("[AUDIT_QUERY_COMPLETED] Retrieved {Count} fraud alert record(s)", results.Count);

        return results;
    }
}
