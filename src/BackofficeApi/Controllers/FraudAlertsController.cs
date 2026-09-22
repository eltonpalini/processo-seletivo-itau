using FraudMonitor.BackofficeApi.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FraudMonitor.BackofficeApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "FraudAnalyst")]
public class FraudAlertsController : ControllerBase
{
    private readonly GetFraudAlertsQueryHandler _queryHandler;

    public FraudAlertsController(GetFraudAlertsQueryHandler queryHandler)
    {
        _queryHandler = queryHandler;
    }

    /// <summary>
    /// Queries flagged transactions with optional filters: status, date range and risk level.
    /// Requires Bearer JWT with FraudAnalyst role.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAlerts([FromQuery] GetFraudAlertsQuery query)
    {
        var results = await _queryHandler.HandleAsync(query);
        return Ok(results);
    }
}
