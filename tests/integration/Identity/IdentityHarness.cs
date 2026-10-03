using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Saas.Subscription.Sample.Application.Configuration;
using Saas.Subscription.Sample.Application.Identity;
using Saas.Subscription.Sample.Domain.Identity;
using Saas.Subscription.Sample.Infrastructure.Identity;
using Saas.Subscription.Sample.Infrastructure.Persistence;

namespace Saas.Subscription.Sample.IntegrationTests.Identity;

/// <summary>
/// The real services over a real, migrated PostgreSQL database. The work factor is the OWASP floor to keep the
/// suite fast; the hasher is wrapped only to count calls, never to change its behavior.
/// </summary>
internal sealed class IdentityHarness
{
    public const string Email = "ana@example.com";
    public const string Password = "right-password";
    public const string OtherPassword = "wrong-password";

    private const int Memory = PasswordHashingOptions.MinMemoryMebibytes;
    private const int Iterations = PasswordHashingOptions.MinIterations;

    private readonly LoginThrottlePolicy _throttlePolicy;

    private IdentityHarness(string connectionString, LoginThrottlePolicy throttlePolicy, int iterations, FakeTimeProvider time)
    {
        ConnectionString = connectionString;
        _throttlePolicy = throttlePolicy;
        Time = time;
        Hasher = new CountingPasswordHasher(CreateHasher(iterations));
        Throttle = new InMemoryLoginThrottle(Time, throttlePolicy);
        Dummy = new DummyPasswordHash(Hasher);
    }

    public string ConnectionString { get; }

    public FakeTimeProvider Time { get; }

    public CountingPasswordHasher Hasher { get; }

    public InMemoryLoginThrottle Throttle { get; }

    public DummyPasswordHash Dummy { get; }

    public static async Task<IdentityHarness> CreateAsync(Persistence.PostgresFixture postgres, LoginThrottlePolicy? throttlePolicy = null)
    {
        var database = await postgres.CreateMigratedDatabaseAsync();
        var time = new FakeTimeProvider(DateTimeOffset.Parse("2026-10-03T12:00:00Z", System.Globalization.CultureInfo.InvariantCulture));
        return new IdentityHarness(database.ConnectionString, throttlePolicy ?? LoginThrottlePolicy.Default, Iterations, time);
    }

    /// <summary>The same database and clock with a different current work factor, as after a configuration change.</summary>
    public IdentityHarness WithIterations(int iterations) => new(ConnectionString, _throttlePolicy, iterations, Time);

    public static Argon2idPasswordHasher CreateHasher(int iterations) =>
        new(Options.Create(new PasswordHashingOptions { MemoryMebibytes = Memory, Iterations = iterations }));

    public AppDbContext CreateContext() =>
        new(AppDbContext.ConfigureOptions(new DbContextOptionsBuilder<AppDbContext>(), ConnectionString).Options);

    /// <summary>A service instance owns its context, like one request does; callers dispose it with the result.</summary>
    public async Task<RegistrationResult> RegisterAsync(string email = Email, string password = Password)
    {
        await using var context = CreateContext();
        return await new RegistrationService(context, Hasher, Time).RegisterAsync(email, password, CancellationToken.None);
    }

    public async Task<LoginResult> LoginAsync(string email = Email, string password = Password)
    {
        await using var context = CreateContext();
        var service = new LoginService(context, Hasher, Dummy, Throttle, Time, NullLogger<LoginService>.Instance);
        return await service.LoginAsync(email, password, CancellationToken.None);
    }
}

internal sealed class CountingPasswordHasher(IPasswordHasher inner) : IPasswordHasher
{
    private int _hashCalls;
    private int _verifyCalls;

    public int HashCalls => Volatile.Read(ref _hashCalls);

    public int VerifyCalls => Volatile.Read(ref _verifyCalls);

    public StoredPasswordHash Hash(string password)
    {
        Interlocked.Increment(ref _hashCalls);
        return inner.Hash(password);
    }

    public PasswordVerificationResult Verify(string password, StoredPasswordHash stored)
    {
        Interlocked.Increment(ref _verifyCalls);
        return inner.Verify(password, stored);
    }
}
