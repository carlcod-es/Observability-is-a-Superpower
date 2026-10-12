var builder = DistributedApplication.CreateBuilder(args);

// Candidate switch (design D7): none | b. Read while the model is built.
var candidates = (builder.Configuration["Spike:Candidates"] ?? "b").ToLowerInvariant();
if (candidates is not ("none" or "b"))
{
    throw new InvalidOperationException(
        $"Invalid Spike:Candidates value '{candidates}'. Valid values: none, b.");
}
var runB = candidates is "b";

// Telemetry goes apps -> collector -> dashboard (+ candidate B backend). WithAppForwarding re-points the
// projects' OTLP endpoint at the collector and gives it the dashboard endpoint and API key.
// The candidate exporter is an extra config file (extra pipeline keys merge into the base pipelines).
var collector = builder.AddOpenTelemetryCollector("collector", c => c.CollectorTag = "0.162.0")
    .WithConfig("../collector/config.yaml")
    .WithAppForwarding();

// Candidate B: Grafana LGTM + Grafana MCP server (read-only).
if (runB)
{
    var lgtm = builder.AddContainer("lgtm", "grafana/otel-lgtm", "0.36.0")
        .WithHttpEndpoint(port: 3000, targetPort: 3000, name: "grafana")
        .WithHttpEndpoint(targetPort: 4318, name: "otlp-http");

    collector.WithConfig("../collector/config-b.yaml")
        .WithEnvironment("LGTM_OTLP_URL", lgtm.GetEndpoint("otlp-http"))
        .WaitFor(lgtm);

    builder.AddContainer("grafana-mcp", "grafana/mcp-grafana", "2.0.2")
        // Host validation defaults to loopback variants of --address; allow the published host port.
        .WithArgs("-t", "streamable-http", "--address", "0.0.0.0:8000", "--disable-write",
            "--allowed-hosts", "localhost:8091,127.0.0.1:8091")
        .WithEnvironment("GRAFANA_URL", lgtm.GetEndpoint("grafana"))
        .WithEnvironment("GRAFANA_USERNAME", "admin")
        .WithEnvironment("GRAFANA_PASSWORD", "admin")
        .WithHttpEndpoint(port: 8091, targetPort: 8000, name: "mcp")
        .WaitFor(lgtm);
}

var db = builder.AddAzureSqlServer("sql")
    .RunAsContainer()
    .AddDatabase("probe");

var api = builder.AddProject<Projects.Probe_Api>("probe-api")
    .WithReference(db)
    .WaitFor(db);

builder.AddProject<Projects.Probe_Web>("probe-web")
    .WithReference(api)
    .WaitFor(api)
    .WithExternalHttpEndpoints();

builder.Build().Run();
