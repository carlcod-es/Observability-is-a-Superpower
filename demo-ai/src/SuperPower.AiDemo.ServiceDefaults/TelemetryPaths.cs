using Microsoft.AspNetCore.Http;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// ADR-0008: one predicate for "not worth telemetry" requests (health, static assets). Used by the trace
/// filter and by the Serilog forwarding sink so filtered requests leave neither spans nor orphaned logs.
/// </summary>
internal static class TelemetryPaths
{
    private static readonly string[] ExcludedPrefixes =
        ["/health", "/alive", "/umbraco/backoffice", "/css", "/js", "/media", "/assets","/favicon.ico"];

    public static bool IsExcluded(PathString path) =>
        ExcludedPrefixes.Any(p => path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase));

    public static bool IsExcluded(string? path) => path is not null && IsExcluded(new PathString(path));
}
