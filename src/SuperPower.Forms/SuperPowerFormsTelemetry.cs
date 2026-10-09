using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace SuperPower.Forms;

/// <summary>
/// Telemetry sources of the package. BCL only (ADR-0006); the host registers the name with its exporter pipeline.
/// </summary>
public static class SuperPowerFormsTelemetry
{
    public const string Name = "SuperPower.Forms";

    public static readonly ActivitySource ActivitySource = new(Name);

    public static readonly Meter Meter = new(Name);
}
