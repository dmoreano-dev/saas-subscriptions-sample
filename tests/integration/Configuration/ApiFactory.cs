using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Saas.Subscription.Sample.IntegrationTests.Configuration;

/// <summary>
/// The real API in Development. The connection string is a placeholder: the API does not open a database
/// connection yet, and Aspire supplies the real one locally.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public const string PlaceholderConnectionString = "Host=localhost;Database=unused;Username=unused;Password=unused";

    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.UseSetting("ConnectionStrings:appdb", PlaceholderConnectionString);
}
