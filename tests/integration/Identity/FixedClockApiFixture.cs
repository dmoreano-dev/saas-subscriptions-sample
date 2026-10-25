using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Saas.Subscription.Sample.Application.Authorization;
using Saas.Subscription.Sample.Application.Configuration;
using Saas.Subscription.Sample.Domain.Identity;
using Saas.Subscription.Sample.IntegrationTests.Configuration;

namespace Saas.Subscription.Sample.IntegrationTests.Identity;

/// <summary>
/// The real API (no database) with a controllable clock. Token validation happens before any handler runs, so one
/// protected route is enough to observe it: <b>200</b> means the token was accepted and the request reached the
/// handler, <b>401</b> means it was refused. Only the database read behind that handler is replaced, by a caller who
/// exists and has no accounts; authorization against real PostgreSQL is covered in <c>Authorization/</c>.
/// </summary>
public sealed class FixedClockApiFixture : IDisposable
{
    public const string ProtectedRoute = "/api/accounts";

    public static readonly DateTimeOffset Start = new(2026, 10, 3, 12, 0, 0, TimeSpan.Zero);

    private readonly ApiFactory _root = new();

    public FixedClockApiFixture() =>
        Factory = _root.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.Replace(ServiceDescriptor.Singleton<TimeProvider>(Clock));
            services.Replace(ServiceDescriptor.Scoped<IAccountAccessReader, ActiveUserWithoutAccounts>());
        }));

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

internal sealed class ActiveUserWithoutAccounts : IAccountAccessReader
{
    public Task<AccountAccessFacts?> FindAsync(Guid userId, Guid accountId, CancellationToken cancellationToken) =>
        Task.FromResult<AccountAccessFacts?>(new AccountAccessFacts(UserStatus.Active, Membership: null));

    public Task<UserMemberships?> ListMembershipsAsync(Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult<UserMemberships?>(new UserMemberships(UserStatus.Active, []));
}
