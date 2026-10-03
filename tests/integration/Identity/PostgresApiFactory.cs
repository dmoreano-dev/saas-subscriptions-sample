using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Saas.Subscription.Sample.IntegrationTests.Identity;

/// <summary>The real API in Development over a real, migrated database, with the OWASP-floor work factor for speed.</summary>
internal sealed class PostgresApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:appdb", connectionString);
        builder.UseSetting("PasswordHashing:MemoryMebibytes", "19");
        builder.UseSetting("PasswordHashing:Iterations", "2");
    }
}
