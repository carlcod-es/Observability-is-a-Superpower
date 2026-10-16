var builder = DistributedApplication.CreateBuilder(args);

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
