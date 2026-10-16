using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Data.SqlClient;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddOpenTelemetry()
    // Probe-only: ServiceDefaults does not register SqlClient metrics.
    .WithMetrics(m => m.AddMeter("Probe.Api").AddSqlClientInstrumentation())
    .WithTracing(t => t.AddSource("Probe.Api"));

var activitySource = new ActivitySource("Probe.Api");
var meter = new Meter("Probe.Api");
var visitsCounter = meter.CreateCounter<long>("probe.visits");
var durationHistogram = meter.CreateHistogram<double>("probe.visit.duration", unit: "ms");

var app = builder.Build();
app.MapDefaultEndpoints();

var connectionString = app.Configuration.GetConnectionString("probe")
    ?? throw new InvalidOperationException("ConnectionStrings:probe missing");

Console.WriteLine("Probe.Api starting");

// Retry: the database resource may report ready slightly before it accepts logins.
for (var attempt = 1; ; attempt++)
{
    try
    {
        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();
        await using var cmd = new SqlCommand("""
            IF OBJECT_ID(N'dbo.visit', N'U') IS NULL
            CREATE TABLE dbo.visit (
                id INT IDENTITY(1,1) PRIMARY KEY,
                path NVARCHAR(400) NOT NULL,
                created DATETIME2 NOT NULL
            )
            """, conn);
        await cmd.ExecuteNonQueryAsync();
        break;
    }
    catch (SqlException) when (attempt < 10)
    {
        await Task.Delay(3000);
    }
}

app.MapGet("/visits", async (HttpContext ctx, int? slow, bool? fail, ILogger<Program> logger) =>
{
    var sw = Stopwatch.StartNew();
    var path = ctx.Request.Path + ctx.Request.QueryString;

    await using var conn = new SqlConnection(connectionString);
    await conn.OpenAsync();

    await using (var insert = new SqlCommand("INSERT INTO dbo.visit (path, created) VALUES (@path, SYSUTCDATETIME())", conn))
    {
        insert.Parameters.AddWithValue("@path", path);
        await insert.ExecuteNonQueryAsync();
    }

    if (slow is > 0)
    {
        using var span = activitySource.StartActivity("probe.slow-work");
        span?.SetTag("probe.delay_ms", slow.Value);
        await Task.Delay(slow.Value);
    }

    if (fail == true)
    {
        throw new InvalidOperationException("Probe failure requested");
    }

    await using var count = new SqlCommand("SELECT COUNT(*) FROM dbo.visit", conn);
    var visitCount = (int)(await count.ExecuteScalarAsync())!;

    visitsCounter.Add(1);
    durationHistogram.Record(sw.Elapsed.TotalMilliseconds);
    logger.LogInformation("Visit recorded for {Path}, total {VisitCount}", path, visitCount);

    return Results.Ok(new { count = visitCount });
});

app.Run();
