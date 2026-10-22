using SuperPower.Web.Telemetry;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

#if DEBUG
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);
#endif

builder.CreateUmbracoBuilder()
    .AddBackOffice()
    .AddWebsite()
    .AddComposers()
    .Build();

builder.AddServiceDefaults();

if (!string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("queue")))
{
    builder.AddRabbitMQClient("queue", s => s.DisableHealthChecks = true);
}

WebApplication app = builder.Build();

app.MapDefaultEndpoints();

await app.BootUmbracoAsync();

app.UseMiddleware<PageTypeTelemetryMiddleware>();

app.UseUmbraco()
    .WithMiddleware(u =>
    {
        u.UseBackOffice();
        u.UseWebsite();
    })
    .WithEndpoints(u =>
    {
        u.UseBackOfficeEndpoints();
        u.UseWebsiteEndpoints();
    });

await app.RunAsync();
