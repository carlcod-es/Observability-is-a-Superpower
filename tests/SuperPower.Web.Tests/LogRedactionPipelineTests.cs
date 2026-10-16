using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using OpenTelemetry.Logs;
using Serilog;
using Serilog.Core;
using Xunit;

namespace SuperPower.Web.Tests;

// ADR-0028: Serilog -> SerilogForwardingSink -> ConfigureOpenTelemetry's processors. Commenting out the
// marked AddProcessor line in Extensions.cs makes the canaries appear and this test fail.
public class LogRedactionPipelineTests
{
    private sealed record Model(string Name, string Email, string Message);

    private sealed class CapturingProcessor : BaseProcessor<LogRecord>
    {
        public List<(Dictionary<string, object?> Attributes, string? Message)> Records { get; } = [];

        public override void OnEnd(LogRecord record) =>
            Records.Add((record.Attributes?.ToDictionary(a => a.Key, a => a.Value) ?? [], record.FormattedMessage));
    }

    [Fact]
    public void Destructured_form_data_is_masked_before_later_processors()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Telemetry:RedactionKey"] = "test-key",
            ["OTEL_EXPORTER_OTLP_ENDPOINT"] = null,
            ["APPLICATIONINSIGHTS_CONNECTION_STRING"] = null,
        });
        builder.ConfigureOpenTelemetry();

        var capture = new CapturingProcessor();
        builder.Services.ConfigureOpenTelemetryLoggerProvider(p => p.AddProcessor(capture));

        using var host = builder.Build();
        var sink = host.Services.GetRequiredService<ILogEventSink>();
        using var logger = new LoggerConfiguration().WriteTo.Sink(sink).CreateLogger();

        logger.Information("Received {@Model}", new Model("Canary Person", "canary@test.invalid", "Canary \"quoted\" message"));

        var (attributes, message) = Assert.Single(capture.Records);
        Assert.Equal("[REDACTED]", attributes["Model.Name"]);
        Assert.Equal("[REDACTED]", attributes["Model.Message"]);
        Assert.StartsWith("[REDACTED:", (string?)attributes["Model.Email"]);

        string[] canaries = ["Canary Person", "canary@test.invalid", "Canary", "quoted"];
        foreach (var canary in canaries)
        {
            Assert.DoesNotContain(attributes.Values, v => v is string s && s.Contains(canary));
            Assert.DoesNotContain(canary, message);
        }
    }
}
