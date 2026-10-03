using System.Net.Http.Json;
using System.Text.Json;
using Saas.Subscription.Sample.IntegrationTests.Persistence;

namespace Saas.Subscription.Sample.IntegrationTests.Identity;

/// <summary>The HTTP calls the authentication tests share, against the real API over a real database.</summary>
internal static class AuthApi
{
    public const string RegisterRoute = "/api/auth/register";
    public const string LoginRoute = "/api/auth/login";
    public const string MeRoute = "/api/me";
    public const string Email = "ana@example.com";
    public const string Password = "right-password";
    public const string WrongPassword = "wrong-password";
    public const string UnknownEmail = "nobody@example.com";

    public static Task<HttpResponseMessage> LoginAsync(HttpClient client, string? email = Email, string? password = Password) =>
        client.PostAsJsonAsync(LoginRoute, new { email, password });

    public static async Task RegisterAsync(HttpClient client, string email = Email, string password = Password)
    {
        using var response = await client.PostAsJsonAsync(RegisterRoute, new { email, password });
        response.EnsureSuccessStatusCode();
    }

    /// <summary>Registers the default user and signs in, returning the access token.</summary>
    public static async Task<string> RegisterAndLoginAsync(HttpClient client)
    {
        await RegisterAsync(client);
        using var response = await LoginAsync(client);
        response.EnsureSuccessStatusCode();
        return (await ReadJsonAsync(response)).GetProperty("accessToken").GetString()!;
    }

    public static Task<HttpResponseMessage> GetMeAsync(HttpClient client, string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, MeRoute) { Headers = { Authorization = new("Bearer", token) } };
        return client.SendAsync(request);
    }

    public static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.Clone();
    }

    public static async Task FailLoginsAsync(HttpClient client, int attempts)
    {
        for (var attempt = 0; attempt < attempts; attempt++)
        {
            using var response = await LoginAsync(client, password: WrongPassword);
        }
    }

    public static async Task SetUserStatusAsync(string connectionString, string status)
    {
        await using var connection = await Sql.OpenAsync(connectionString);
        await Sql.ExecuteAsync(connection, "UPDATE users SET status = @status", ("status", status));
    }
}
