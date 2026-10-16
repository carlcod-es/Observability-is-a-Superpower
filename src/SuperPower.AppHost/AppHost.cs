using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;

// Host port for smtp4dev SMTP; the site connects here directly.
const int SmtpPort = 2525;

var builder = DistributedApplication.CreateBuilder(args);

var db = builder.AddAzureSqlServer("sql")
    .RunAsContainer(c => c
        .WithDataVolume()
        .WithLifetime(ContainerLifetime.Persistent))
    .AddDatabase("umbraco");

// Mail catcher: web UI on 80 (dashboard link), SMTP published on host 2525 -> container 25, not proxied by Aspire.
var smtp4dev = builder.AddContainer("smtp4dev", "rnwood/smtp4dev", "3.15.0")
    .WithHttpEndpoint(targetPort: 80, name: "http")
    .WithEndpoint(port: SmtpPort, targetPort: 25, name: "smtp", scheme: "tcp", isProxied: false)
    .WithEnvironment("ServerOptions__Port", "25")
    .WithEnvironment("ServerOptions__TlsMode", "None")
    .WithEnvironment("ServerOptions__Database", "") // empty = in-memory store
    .WithEnvironment("ServerOptions__LockSettings", "true")
    .WithUrlForEndpoint("http", u => u.DisplayText = "Inbox")
    .WithExternalHttpEndpoints();

// Telemetry: apps -> collector -> Aspire dashboard + Grafana LGTM (ADR-0017, ADR-0022).
// LGTM is started on demand (explicit start) and receives only post-start telemetry; grafana-mcp waits for it.
// Demo dashboards are file-provisioned via read-only bind mounts (ADR-0023).
// WithAppForwarding re-points the projects' OTLP endpoint at the collector.
var lgtm = builder.AddContainer("lgtm", "grafana/otel-lgtm", "0.36.0")
    .WithHttpEndpoint(port: 3000, targetPort: 3000, name: "grafana")
    .WithHttpEndpoint(targetPort: 4318, name: "otlp-http")
    .WithHttpHealthCheck("/api/health", endpointName: "grafana")
    .WithBindMount("./grafana/acme-dashboards.yaml", "/otel-lgtm/grafana/conf/provisioning/dashboards/acme-dashboards.yaml", isReadOnly: true)
    .WithBindMount("./grafana/dashboards", "/otel-lgtm/acme-dashboards", isReadOnly: true)
    .WithExplicitStart();

var collector = builder.AddOpenTelemetryCollector("collector", c => c.CollectorTag = "0.162.0")
    .WithConfig("./collector/config.yaml")
    .WithAppForwarding()
    .WithEnvironment("LGTM_OTLP_URL", lgtm.GetEndpoint("otlp-http"));

// Read-only Grafana MCP for the agent. Host validation defaults to loopback variants of --address,
// so allow the published host port.
builder.AddContainer("grafana-mcp", "grafana/mcp-grafana", "2.0.2")
    .WithArgs("-t", "streamable-http", "--address", "0.0.0.0:8000", "--disable-write",
        "--enabled-tools", "prometheus,loki,tempo,datasource",
        "--allowed-hosts", "localhost:8091,127.0.0.1:8091")
    .WithEnvironment("GRAFANA_URL", lgtm.GetEndpoint("grafana"))
    .WithEnvironment("GRAFANA_USERNAME", "admin")
    .WithEnvironment("GRAFANA_PASSWORD", "admin")
    .WithHttpEndpoint(port: 8091, targetPort: 8000, name: "mcp")
    .WaitFor(lgtm);

// Pseudonymisation key for log redaction (ADR-0028); generated once, persisted in AppHost user secrets.
var redactionKey = builder.AddParameter("telemetry-redaction-key",
    new GenerateParameterDefault { MinLength = 32, Special = false }, secret: true, persist: true);

builder.AddProject<Projects.SuperPower_Web>("site")
    .WithReference(db, connectionName: "umbracoDbDSN")
    .WithEnvironment("ConnectionStrings__umbracoDbDSN_ProviderName", "Microsoft.Data.SqlClient")
    .WithEnvironment("Umbraco__CMS__Global__Smtp__Host", "localhost")
    .WithEnvironment("Umbraco__CMS__Global__Smtp__Port", SmtpPort.ToString())
    .WithEnvironment("Umbraco__CMS__Global__Smtp__SecureSocketOptions", "None")
    .WithEnvironment("Umbraco__CMS__Global__Smtp__From", "noreply@acme-agency.example")
    .WithEnvironment("Acme__Forms__OwnerEmail", "owner@acme-agency.example")
    .WithEnvironment("Telemetry__RedactionKey", redactionKey)
    .WaitFor(db)
    .WaitFor(smtp4dev)
    .WaitFor(collector)
    .WithExternalHttpEndpoints();

builder.Build().Run();
