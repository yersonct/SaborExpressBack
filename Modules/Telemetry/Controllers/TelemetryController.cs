using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaborExpress.Modules.Telemetry.DTOs;
using SaborExpress.Shared.Observability;

namespace SaborExpress.Modules.Telemetry.Controllers;

[ApiController]
[Authorize]
[Route("api/telemetry")]
public sealed class TelemetryController : ControllerBase
{
    private const int MaxBatchSize = 100;
    private const double MaxDurationMs = 120_000;
    private readonly ApiMetricsRegistry _metrics;
    private readonly ApiRouteCatalog _routeCatalog;

    public TelemetryController(ApiMetricsRegistry metrics, ApiRouteCatalog routeCatalog)
    {
        _metrics = metrics;
        _routeCatalog = routeCatalog;
    }

    [HttpPost("client-latency")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult RecordClientLatency([FromBody] ClientLatencyBatchDto batch)
    {
        if (batch.Measurements is null || batch.Measurements.Count is 0 or > MaxBatchSize)
            return BadRequest();

        foreach (var item in batch.Measurements)
        {
            if (!_routeCatalog.Contains(item.Route) ||
                item.StatusCode is < 100 or > 599 ||
                !double.IsFinite(item.ClientDurationMs) ||
                !double.IsFinite(item.ServerDurationMs) ||
                item.ClientDurationMs is < 0 or > MaxDurationMs ||
                item.ServerDurationMs is < 0 or > MaxDurationMs ||
                item.ServerDurationMs > item.ClientDurationMs)
                return BadRequest();
        }

        foreach (var item in batch.Measurements)
        {
            _metrics.RecordClientNetwork(
                item.Method,
                item.Route,
                item.StatusCode,
                item.ClientDurationMs - item.ServerDurationMs);
        }

        return NoContent();
    }
}
