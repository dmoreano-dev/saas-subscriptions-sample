using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using Saas.Subscription.Sample.IntegrationTests.Configuration;
using Saas.Subscription.Sample.IntegrationTests.Persistence;

namespace Saas.Subscription.Sample.IntegrationTests.Identity;

[Collection(PostgresTestGroup.Name)]
public class LogRedactionTests(PostgresFixture postgres)
{
    private const string TamperedSuffix = "x";
    private const string GarbageToken = "not.a.jwt";

    [Fact]
    public async Task AuthenticationFlows_ThroughTheRealHost_LogNoSecretAndReturnNone()
    {
        // Arrange
        var database = await postgres.CreateMigratedDatabaseAsync();
        var logs = new CapturingLoggerProvider();
        await using var factory = new LoggingFactory(database.ConnectionString, logs);
        using var client = factory.CreateClient();
        var storedHash = string.Empty;
        var responses = new List<string>();

        // Act
        var token = await AuthApi.RegisterAndLoginAsync(client);
        responses.Add(token);
        await using (var connection = await Sql.OpenAsync(database.ConnectionString))
        {
            storedHash = await Sql.ScalarAsync<string>(connection, "SELECT encoded_hash FROM password_credentials");
        }

        responses.Add(await BodyAsync(await AuthApi.LoginAsync(client, password: AuthApi.WrongPassword)));
        responses.Add(await BodyAsync(await AuthApi.LoginAsync(client, AuthApi.UnknownEmail)));
        responses.Add(await BodyAsync(await AuthApi.GetMeAsync(client, token + TamperedSuffix)));
        responses.Add(await BodyAsync(await AuthApi.GetMeAsync(client, GarbageToken)));
        responses.Add(await BodyAsync(await client.PostAsJsonAsync(AuthApi.RegisterRoute, new { email = "bad", password = AuthApi.Password })));
        await AuthApi.FailLoginsAsync(client, 6);
        responses.Add(await BodyAsync(await AuthApi.LoginAsync(client)));
        responses.Add(await BodyAsync(await AuthApi.GetMeAsync(client, token)));

        // Assert
        var secrets = new Dictionary<string, string>
        {
            ["password"] = AuthApi.Password,
            ["wrong password"] = AuthApi.WrongPassword,
            ["access token"] = token,
            ["token signature"] = token[(token.LastIndexOf('.') + 1)..],
            ["signing key"] = PemBody(TestSigningKey.Pem),
            ["password hash"] = storedHash,
            ["database password"] = new NpgsqlConnectionStringBuilder(database.ConnectionString).Password!,
        };
        var allLogs = string.Join('\n', logs.Entries);

        Assert.NotEmpty(logs.Entries);
        foreach (var (name, secret) in secrets)
        {
            Assert.False(allLogs.Contains(secret, StringComparison.Ordinal), $"The logs contain the {name}: " + string.Join(" || ", logs.Entries.Where(e => e.Contains(secret, StringComparison.Ordinal)).Select(e => e.Replace("\n", " | "))));
        }

        // The responses to failures must not echo credentials, keys or hashes either (the token is the login's own result).
        foreach (var body in responses.Skip(1))
        {
            foreach (var name in new[] { "wrong password", "signing key", "password hash", "database password" })
            {
                Assert.False(body.Contains(secrets[name], StringComparison.Ordinal), $"A response contains the {name}.");
            }
        }
    }

    private static async Task<string> BodyAsync(HttpResponseMessage response)
    {
        using (response)
        {
            return await response.Content.ReadAsStringAsync();
        }
    }

    private static string PemBody(string pem) =>
        string.Concat(pem.Split('\n').Where(line => !line.StartsWith("-----", StringComparison.Ordinal)).Select(line => line.Trim()));

    /// <summary>The real API with every log level captured, including framework and token-library categories.</summary>
    private sealed class LoggingFactory(string connectionString, CapturingLoggerProvider logs) : Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("ConnectionStrings:appdb", connectionString);
            builder.UseSetting("PasswordHashing:MemoryMebibytes", "19");
            builder.UseSetting("PasswordHashing:Iterations", "2");
            builder.UseSetting("Authentication:SigningKey", TestSigningKey.Pem);
            builder.UseSetting("Authentication:SigningKeyId", TestSigningKey.KeyId);
            builder.ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.SetMinimumLevel(LogLevel.Trace);
                logging.AddProvider(logs);
            });
        }
    }
}
