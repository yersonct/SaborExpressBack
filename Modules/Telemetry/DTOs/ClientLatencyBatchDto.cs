namespace SaborExpress.Modules.Telemetry.DTOs;

public sealed class ClientLatencyBatchDto
{
    public List<ClientLatencyObservationDto> Measurements { get; init; } = [];
}

public sealed class ClientLatencyObservationDto
{
    public string Method { get; init; } = string.Empty;
    public string Route { get; init; } = string.Empty;
    public int StatusCode { get; init; }
    public double ClientDurationMs { get; init; }
    public double ServerDurationMs { get; init; }
}
