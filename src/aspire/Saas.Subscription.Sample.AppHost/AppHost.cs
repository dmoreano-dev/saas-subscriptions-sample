var builder = DistributedApplication.CreateBuilder(args);


var postgresPassword = builder.AddParameter(
    "postgres-password",
    new GenerateParameterDefault { MinLength = 24, Special = false },
    secret: true,
    persist: true);

var postgres = builder.AddPostgres("postgres", password: postgresPassword)
    .WithImageTag("17")
    .WithDataVolume("saas-sample-pgdata");
var database = postgres.AddDatabase("appdb", "saas_sample");

// Migrations are a controlled step that runs to completion, never part of the API's startup path.
// For now a single database login is used for migrations and (later) the API; separate migration and
// runtime roles are a hosted-database hardening step (DEP-01).
var migrator = builder.AddProject<Projects.Saas_Subscription_Sample_Migrator>("migrator")
    .WithReference(database)
    .WaitFor(database);

var api = builder.AddProject<Projects.Saas_Subscription_Sample_Api>("api")
    .WaitForCompletion(migrator);

// The Vite dev server proxies /api to the API using the service-discovery variables Aspire injects.
builder.AddViteApp("frontend", "../../frontend")
    .WithReference(api)
    .WaitFor(api);

builder.Build().Run();
