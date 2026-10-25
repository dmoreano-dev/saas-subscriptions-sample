using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;
using Saas.Subscription.Sample.Application.Configuration;
using Saas.Subscription.Sample.IntegrationTests.Identity;
using Saas.Subscription.Sample.IntegrationTests.Persistence;

namespace Saas.Subscription.Sample.IntegrationTests.Authorization;

/// <summary>A registered, signed-in user with their personal account.</summary>
internal sealed record Caller(string Token, Guid UserId, Guid AccountId);

/// <summary>
/// The real API over a real, migrated PostgreSQL database with two users, Ana and Bob, each registered through the
/// API (so each owns one personal account) and each with one project. Ana is the caller under test; everything of
/// Bob's is foreign to her.
/// </summary>
internal sealed class TwoAccounts : IAsyncDisposable
{
    public const string AnaEmail = "ana@example.com";
    public const string BobEmail = "bob@example.com";
    public const string AnaProjectName = "Ana's project";
    public const string BobProjectName = "Bob's project";

    private const string ProjectJson = """{"name":"p","description":null}""";

    private readonly PostgresApiFactory _factory;

    private TwoAccounts(PostgresApiFactory factory, HttpClient client, string connectionString)
    {
        _factory = factory;
        Client = client;
        ConnectionString = connectionString;
    }

    public HttpClient Client { get; }

    public string ConnectionString { get; }

    public Caller Ana { get; private set; } = null!;

    public Caller Bob { get; private set; } = null!;

    public Guid AnaProjectId { get; private set; }

    public Guid BobProjectId { get; private set; }

    public AuthenticationOptions TokenOptions => _factory.Services.GetRequiredService<IOptions<AuthenticationOptions>>().Value;

    public static async Task<TwoAccounts> CreateAsync(PostgresFixture postgres)
    {
        var database = await postgres.CreateMigratedDatabaseAsync();
        var factory = new PostgresApiFactory(database.ConnectionString);
        var world = new TwoAccounts(factory, factory.CreateClient(), database.ConnectionString);

        world.Ana = await RegisterAndLoginAsync(world.Client, AnaEmail);
        world.Bob = await RegisterAndLoginAsync(world.Client, BobEmail);

        await using var connection = await world.OpenAsync();
        world.AnaProjectId = await Sql.InsertProjectAsync(connection, world.Ana.AccountId, AnaProjectName);
        world.BobProjectId = await Sql.InsertProjectAsync(connection, world.Bob.AccountId, BobProjectName);
        return world;
    }

    public static string AccountsRoute => "/api/accounts";

    public static string CapabilitiesRoute(Guid accountId) => $"/api/accounts/{accountId}/capabilities";

    public static string ProjectsRoute(Guid accountId) => $"/api/accounts/{accountId}/projects";

    public static string ProjectRoute(Guid accountId, Guid projectId) => $"/api/accounts/{accountId}/projects/{projectId}";

    public Task<NpgsqlConnection> OpenAsync() => Sql.OpenAsync(ConnectionString);

    public Task<HttpResponseMessage> SendAsync(HttpMethod method, string route, string token)
    {
        var request = new HttpRequestMessage(method, route) { Headers = { Authorization = new("Bearer", token) } };
        if (method == HttpMethod.Post)
        {
            request.Content = new StringContent(ProjectJson, System.Text.Encoding.UTF8, "application/json");
        }

        return Client.SendAsync(request);
    }

    /// <summary>Runs one statement that changes the state the API must notice on the next request.</summary>
    public async Task ExecuteAsync(string sql, params (string Name, object? Value)[] parameters)
    {
        await using var connection = await OpenAsync();
        await Sql.ExecuteAsync(connection, sql, parameters);
    }

    public async Task<long> CountProjectsAsync(Guid projectId)
    {
        await using var connection = await OpenAsync();
        return await Sql.ScalarAsync<long>(connection, "SELECT count(*) FROM projects WHERE id = @id", ("id", projectId));
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await _factory.DisposeAsync();
    }

    private static async Task<Caller> RegisterAndLoginAsync(HttpClient client, string email)
    {
        using var registered = await client.PostAsJsonAsync(AuthApi.RegisterRoute, new { email, password = AuthApi.Password });
        registered.EnsureSuccessStatusCode();
        var accountId = (await AuthApi.ReadJsonAsync(registered)).GetProperty("personalAccount").GetProperty("id").GetGuid();

        using var signedIn = await AuthApi.LoginAsync(client, email);
        signedIn.EnsureSuccessStatusCode();
        var login = await AuthApi.ReadJsonAsync(signedIn);
        return new Caller(login.GetProperty("accessToken").GetString()!, login.GetProperty("user").GetProperty("id").GetGuid(), accountId);
    }
}
