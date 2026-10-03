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

var mailpit = builder.AddContainer("mailpit", "axllent/mailpit", "v1.31.3")
    .WithEndpoint(name: "smtp", targetPort: 1025)
    .WithHttpEndpoint(name: "ui", targetPort: 8025);
var mailpitSmtp = mailpit.GetEndpoint("smtp");

// Migrations are a controlled step that runs to completion.
var migrator = builder.AddProject<Projects.Saas_Subscription_Sample_Migrator>("migrator")
    .WithReference(database)
    .WaitFor(database);

var api = builder.AddProject<Projects.Saas_Subscription_Sample_Api>("api")
    .WithReference(database)
    .WithEnvironment("Email__Smtp__Host", mailpitSmtp.Property(EndpointProperty.Host))
    .WithEnvironment("Email__Smtp__Port", mailpitSmtp.Property(EndpointProperty.Port))
    .WaitFor(mailpit)
    .WaitForCompletion(migrator);

// The Vite dev server proxies /api to the API using the service-discovery variables Aspire injects.
builder.AddViteApp("frontend", "../../frontend")
    .WithReference(api)
    .WaitFor(api);

builder.Build().Run();
