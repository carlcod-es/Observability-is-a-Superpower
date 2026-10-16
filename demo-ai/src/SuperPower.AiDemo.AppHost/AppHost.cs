var builder = DistributedApplication.CreateBuilder(args);

var db = builder.AddAzureSqlServer("sql")
    .RunAsContainer(c => c.WithDataVolume().WithLifetime(ContainerLifetime.Persistent))
    .AddDatabase("umbraco");

var anthropicKey = builder.AddParameter("anthropic-api-key", secret: true);

builder.AddProject<Projects.SuperPower_AiDemo_Web>("site")
    .WithReference(db, connectionName: "umbracoDbDSN")
    .WithEnvironment("ConnectionStrings__umbracoDbDSN_ProviderName", "Microsoft.Data.SqlClient")
    .WithEnvironment("Umbraco__AI__Secrets__AnthropicApiKey", anthropicKey)
    // Demo Code : AI
    // Prompt and response text on AI spans: "true" to show what would leak, "false" by default
    .WithEnvironment("OTEL_INSTRUMENTATION_GENAI_CAPTURE_MESSAGE_CONTENT", "false")
    .WaitFor(db)
    .WithExternalHttpEndpoints();

builder.Build().Run();
