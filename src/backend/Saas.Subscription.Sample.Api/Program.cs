using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Saas.Subscription.Sample.Api.Endpoints;
using Saas.Subscription.Sample.Api.OpenApi;
using Saas.Subscription.Sample.Api.Problems;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();
builder.Services.AddApiProblemHandling();
builder.Services.AddOpenApi(ApiOpenApiOptions.DocumentName, ApiOpenApiOptions.Configure);

var app = builder.Build();

app.UseApiProblemHandling();

// The document and its UI describe the whole API surface, so they exist only in local development.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// Liveness only reports that the process is running; it intentionally runs no dependency checks.
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });

app.MapApi();

app.Run();

// Exposes the entry point to WebApplicationFactory in the integration tests.
public partial class Program;
