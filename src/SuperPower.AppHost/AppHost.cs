using System.Text;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;

// Default added latency per SMTP server reply for the Slow command (tuned in rehearsal).
// SMTP is lock-step with ~6-7 replies per message, so total delay is roughly 7x this value.
const int SlowLatencyMs = 1000;
const int SmtpProxyPort = 2525;

var builder = DistributedApplication.CreateBuilder(args);

var db = builder.AddAzureSqlServer("sql")
    .RunAsContainer(c => c
        .WithDataVolume()
        .WithLifetime(ContainerLifetime.Persistent))
    .AddDatabase("umbraco");

// Mail catcher: web UI on 80 (dashboard link), SMTP on 25 reachable only inside the container network.
var smtp4dev = builder.AddContainer("smtp4dev", "rnwood/smtp4dev", "3.15.0")
    .WithHttpEndpoint(targetPort: 80, name: "http")
    .WithEndpoint(targetPort: 25, name: "smtp", scheme: "tcp")
    .WithEnvironment("ServerOptions__Port", "25")
    .WithEnvironment("ServerOptions__TlsMode", "None")
    .WithEnvironment("ServerOptions__Database", "") // empty = in-memory store
    .WithEnvironment("ServerOptions__LockSettings", "true")
    .WithUrlForEndpoint("http", u => u.DisplayText = "Inbox")
    .WithExternalHttpEndpoints();

// Fault injection: Toxiproxy API on 8474; proxy "smtp" listens on 2525, not proxied by Aspire
// so connection resets reach the client unchanged.
var toxiproxy = builder.AddContainer("toxiproxy", "ghcr.io/shopify/toxiproxy", "2.12.0")
    .WithHttpEndpoint(targetPort: 8474, name: "api")
    .WithEndpoint(port: SmtpProxyPort, targetPort: SmtpProxyPort, name: "smtp", scheme: "tcp", isProxied: false)
    .WithHttpHealthCheck("/version", endpointName: "api")
    .WaitFor(smtp4dev);

var api = toxiproxy.GetEndpoint("api");

// Create the proxy (populate is idempotent) and start healed.
builder.Eventing.Subscribe<ResourceReadyEvent>(toxiproxy.Resource, async (e, ct) =>
{
    var http = new HttpClient { BaseAddress = new Uri(api.Url) };
    var proxies = $$"""[{"name":"smtp","listen":"0.0.0.0:{{SmtpProxyPort}}","upstream":"smtp4dev:25","enabled":true}]""";
    (await http.PostAsync("/populate", new StringContent(proxies, Encoding.UTF8, "application/json"), ct))
        .EnsureSuccessStatusCode();
    (await http.PostAsync("/reset", null, ct)).EnsureSuccessStatusCode();
});

// Slow/Break delete their toxic first so repeated clicks succeed.
toxiproxy
    .WithHttpCommand("/proxies/smtp/toxics", "Slow", endpointName: "api", commandName: "slow",
        commandOptions: ToxicCommand($"Add {SlowLatencyMs} ms latency to every SMTP reply from the mail server.",
            "slow", $$$"""{"name":"slow","type":"latency","stream":"downstream","toxicity":1.0,"attributes":{"latency":{{{SlowLatencyMs}}},"jitter":0}}"""))
    .WithHttpCommand("/proxies/smtp/toxics", "Break", endpointName: "api", commandName: "break",
        commandOptions: ToxicCommand("Reset every SMTP connection immediately (fast failure).",
            "break", """{"name":"break","type":"reset_peer","stream":"downstream","toxicity":1.0,"attributes":{"timeout":0}}"""))
    .WithHttpCommand("/reset", "Heal", endpointName: "api", commandName: "heal",
        commandOptions: new HttpCommandOptions
        {
            Description = "Remove all toxics and re-enable proxies; mail flows normally.",
            IconName = "Heart"
        });

// Telemetry: apps -> collector -> Aspire dashboard + Grafana LGTM (ADR-0017, ADR-0022).
// LGTM is started on demand (explicit start) and receives only post-start telemetry; grafana-mcp waits for it.
// WithAppForwarding re-points the projects' OTLP endpoint at the collector.
var lgtm = builder.AddContainer("lgtm", "grafana/otel-lgtm", "0.36.0")
    .WithHttpEndpoint(port: 3000, targetPort: 3000, name: "grafana")
    .WithHttpEndpoint(targetPort: 4318, name: "otlp-http")
    .WithHttpHealthCheck("/api/health", endpointName: "grafana")
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

builder.AddProject<Projects.SuperPower_Web>("site")
    .WithReference(db, connectionName: "umbracoDbDSN")
    .WithEnvironment("ConnectionStrings__umbracoDbDSN_ProviderName", "Microsoft.Data.SqlClient")
    .WithEnvironment("Umbraco__CMS__Global__Smtp__Host", "localhost")
    .WithEnvironment("Umbraco__CMS__Global__Smtp__Port", SmtpProxyPort.ToString())
    .WithEnvironment("Umbraco__CMS__Global__Smtp__SecureSocketOptions", "None")
    .WithEnvironment("Umbraco__CMS__Global__Smtp__From", "noreply@acme-agency.example")
    .WaitFor(db)
    .WaitFor(toxiproxy)
    .WaitFor(collector)
    .WithExternalHttpEndpoints();

builder.Build().Run();

static HttpCommandOptions ToxicCommand(string description, string toxicName, string body) => new()
{
    Description = description,
    IconName = "Warning",
    PrepareRequest = async ctx =>
    {
        // Remove an existing toxic of the same name (404 if absent is fine), then add it.
        var uri = new Uri(ctx.Request.RequestUri!, $"/proxies/smtp/toxics/{toxicName}");
        await ctx.HttpClient.SendAsync(new HttpRequestMessage(HttpMethod.Delete, uri), ctx.CancellationToken);
        ctx.Request.Content = new StringContent(body, Encoding.UTF8, "application/json");
    }
};
