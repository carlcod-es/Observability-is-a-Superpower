using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace SuperPower.Web.Forms;

/// <summary>
/// Telemetry sources of the site's forms code (ADR-0016). ServiceDefaults registers the name as a literal.
/// </summary>
public static class AcmeFormsTelemetry
{
    public const string Name = "Acme.Forms";

    public static readonly ActivitySource ActivitySource = new(Name);

    public static readonly Meter Meter = new(Name);
}
