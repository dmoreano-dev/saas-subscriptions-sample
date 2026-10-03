using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Saas.Subscription.Sample.IntegrationTests.Configuration;

/// <summary>
/// The real API in Development without a database: the connection string is a placeholder, so only requests that
/// never reach PostgreSQL can be served (tests that need it use <c>PostgresApiFactory</c>).
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public const string PlaceholderConnectionString = "Host=localhost;Database=unused;Username=unused;Password=unused";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:appdb", PlaceholderConnectionString);
        builder.UseSetting("Authentication:SigningKey", TestSigningKey.Pem);
        builder.UseSetting("Authentication:SigningKeyId", TestSigningKey.KeyId);
    }
}
