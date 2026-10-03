using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Saas.Subscription.Sample.Application.Configuration;
using Saas.Subscription.Sample.IntegrationTests.Configuration;

namespace Saas.Subscription.Sample.IntegrationTests.Identity;

/// <summary>
/// The real API (no database) with a controllable clock. Token validation happens before any handler runs, so a
/// protected route that is still contract-only is enough to observe it: <b>501</b> means the token was accepted
/// and the request reached the handler, <b>401</b> means it was refused.
/// </summary>
public sealed class FixedClockApiFixture : IDisposable
{
    public const string ProtectedRoute = "/api/accounts";

    public static readonly DateTimeOffset Start = new(2026, 10, 3, 12, 0, 0, TimeSpan.Zero);

    private readonly ApiFactory _root = new();

    public FixedClockApiFixture() =>
        Factory = _root.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.Replace(ServiceDescriptor.Singleton<TimeProvider>(Clock))));

    public FakeTimeProvider Clock { get; } = new(Start);

    public WebApplicationFactory<Program> Factory { get; }

    public AuthenticationOptions Options => Factory.Services.GetRequiredService<IOptions<AuthenticationOptions>>().Value;

    public static HttpRequestMessage ProtectedRequest(string token) => new(HttpMethod.Get, ProtectedRoute)
    {
        Headers = { Authorization = new("Bearer", token) },
    };

    public void Dispose()
    {
        Factory.Dispose();
        _root.Dispose();
    }
}
