var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.Saas_Subscription_Sample_Api>("api");

// The Vite dev server proxies /api to the API using the service-discovery variables Aspire injects.
builder.AddViteApp("frontend", "../../frontend")
    .WithReference(api)
    .WaitFor(api);

builder.Build().Run();
