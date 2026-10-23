using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;

// Container and volume names are prefixed oisp- so they are easy to find and reset.

// Host port for smtp4dev SMTP; the site connects here directly.
const int SmtpPort = 2525;

var builder = DistributedApplication.CreateBuilder(args);

var db = builder.AddAzureSqlServer("sql")
    .RunAsContainer(c => c
        .WithContainerName("oisp-sql")
        .WithDataVolume("oisp-sql-data")
        .WithLifetime(ContainerLifetime.Persistent))
    .AddDatabase("umbraco");

// Mail catcher: web UI on 80 (dashboard link), SMTP published on host 2525 -> container 25, not proxied by Aspire.
var smtp4dev = builder.AddContainer("smtp4dev", "rnwood/smtp4dev", "3.15.0")
    .WithContainerName("oisp-smtp4dev")
    .WithHttpEndpoint(targetPort: 80, name: "http")
    .WithEndpoint(port: SmtpPort, targetPort: 25, name: "smtp", scheme: "tcp", isProxied: false)
    .WithEnvironment("ServerOptions__Port", "25")
    .WithEnvironment("ServerOptions__TlsMode", "None")
    .WithEnvironment("ServerOptions__Database", "") // empty = in-memory store
    .WithEnvironment("ServerOptions__LockSettings", "true")
    .WithUrlForEndpoint("http", u => u.DisplayText = "Inbox")
    .WithExternalHttpEndpoints();

// Telemetry: apps -> collector -> Aspire dashboard + Grafana LGTM (ADR-0017, ADR-0022).
// LGTM starts with the AppHost; the collector waits for it so start-up telemetry reaches Grafana (ADR-0033); grafana-mcp waits for it.
// Demo dashboards are file-provisioned via read-only bind mounts (ADR-0023).
// WithAppForwarding re-points the projects' OTLP endpoint at the collector.
var lgtm = builder.AddContainer("lgtm", "grafana/otel-lgtm", "0.36.0")
    .WithContainerName("oisp-lgtm")
    .WithHttpEndpoint(port: 3000, targetPort: 3000, name: "grafana")
    .WithHttpEndpoint(targetPort: 4318, name: "otlp-http")
    .WithHttpHealthCheck("/api/health", endpointName: "grafana")
    .WithBindMount("./grafana/acme-dashboards.yaml", "/otel-lgtm/grafana/conf/provisioning/dashboards/acme-dashboards.yaml", isReadOnly: true)
    .WithBindMount("./grafana/dashboards", "/otel-lgtm/acme-dashboards", isReadOnly: true);

var collector = builder.AddOpenTelemetryCollector("collector", c => c.CollectorTag = "0.162.0")
    .WithContainerName("oisp-collector")
    .WithConfig("./collector/config.yaml")
    .WithAppForwarding()
    .WithEnvironment("LGTM_OTLP_URL", lgtm.GetEndpoint("otlp-http"))
    .WaitFor(lgtm);

// Read-only Grafana MCP for the agent. Host validation defaults to loopback variants of --address,
// so allow the published host port.
builder.AddContainer("grafana-mcp", "grafana/mcp-grafana", "2.0.2")
    .WithContainerName("oisp-grafana-mcp")
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

var cache = builder.AddRedis("cache")
    .WithContainerName("oisp-cache")
    .WithDataVolume("oisp-cache-data")
    .WithLifetime(ContainerLifetime.Persistent);

var queue = builder.AddRabbitMQ("queue")
    .WithContainerName("oisp-queue")
    .WithManagementPlugin()
    .WithLifetime(ContainerLifetime.Persistent);

// Booking service (ADR-0030): no external endpoint, not reachable through the tunnel.
var api = builder.AddProject<Projects.SuperPower_Api>("api")
    .WithReference(cache)
    .WithReference(queue)
    .WithEnvironment("Telemetry__RedactionKey", redactionKey)
    .WaitFor(cache)
    .WaitFor(queue)
    .WaitFor(collector);

builder.AddProject<Projects.SuperPower_Web>("site")
    .WithReference(db, connectionName: "umbracoDbDSN")
    .WithEnvironment("ConnectionStrings__umbracoDbDSN_ProviderName", "Microsoft.Data.SqlClient")
    .WithEnvironment("Umbraco__CMS__Global__Smtp__Host", "localhost")
    .WithEnvironment("Umbraco__CMS__Global__Smtp__Port", SmtpPort.ToString())
    .WithEnvironment("Umbraco__CMS__Global__Smtp__SecureSocketOptions", "None")
    .WithEnvironment("Umbraco__CMS__Global__Smtp__From", "noreply@acme-agency.example")
    .WithEnvironment("Acme__Forms__OwnerEmail", "owner@acme-agency.example")
    .WithEnvironment("Telemetry__RedactionKey", redactionKey)
    .WithReference(api)
    .WithReference(queue) // no WaitFor on api or queue: the site must run without them
    .WaitFor(db)
    .WaitFor(smtp4dev)
    .WaitFor(collector)
    .WithExternalHttpEndpoints();

builder.Build().Run();
