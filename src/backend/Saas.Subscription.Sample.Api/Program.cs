using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();

var app = builder.Build();

// Liveness only reports that the process is running; it intentionally runs no dependency checks.
// Readiness (/health/ready) is added together with the first dependency (PostgreSQL, FND-02).
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });

app.Run();

// Exposes the entry point to WebApplicationFactory in the integration tests.
public partial class Program;
