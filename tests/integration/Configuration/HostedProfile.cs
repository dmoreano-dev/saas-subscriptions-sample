using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Saas.Subscription.Sample.IntegrationTests.Configuration;

/// <summary>A minimal configuration that satisfies every hosted rule, so a test can break exactly one setting.</summary>
public static class HostedProfile
{
    public const string EnvironmentName = "Production";

    private static readonly Dictionary<string, string> ValidSettings = new()
    {
        ["Database:ConnectionString"] = "Host=db.example.test;Database=app;Username=app;Password=secret;SSL Mode=VerifyFull",
        ["Authentication:SigningKey"] = "test-signing-key",
        ["Authentication:SigningKeyId"] = "test-key-id",
        ["Email:Provider"] = "Https",
        ["Email:Https:ApiKey"] = "test-email-api-key",
        ["Frontend:BaseUrl"] = "https://app.example.test",
    };

    /// <summary>Starts the API as a hosted environment with the valid profile, then applies <paramref name="overrides"/>.</summary>
    public static WebApplicationFactory<Program> Create(
        WebApplicationFactory<Program> factory,
        params (string Key, string? Value)[] overrides) =>
        factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(EnvironmentName);
            foreach (var (key, value) in ValidSettings)
            {
                builder.UseSetting(key, value);
            }

            foreach (var (key, value) in overrides)
            {
                builder.UseSetting(key, value);
            }
        });
}
