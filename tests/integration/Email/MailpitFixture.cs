using System.Net.Http.Json;
using System.Text.Json.Serialization;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace Saas.Subscription.Sample.IntegrationTests.Email;

/// <summary>
/// One real Mailpit container (Docker is the only requirement). Its HTTP API is read here, in the tests only:
/// no application code depends on it.
/// </summary>
public sealed class MailpitFixture : IAsyncLifetime
{
    // Keep in sync with the image tag in the AppHost.
    private const string Image = "axllent/mailpit:v1.31.3";
    private const int SmtpPort = 1025;
    private const int HttpPort = 8025;

    private readonly IContainer _container = new ContainerBuilder(Image)
        .WithPortBinding(SmtpPort, assignRandomHostPort: true)
        .WithPortBinding(HttpPort, assignRandomHostPort: true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(request => request.ForPath("/livez").ForPort(HttpPort)))
        .Build();


    public string SmtpHost => _container.Hostname;

    public int SmtpHostPort => _container.GetMappedPublicPort(SmtpPort);

    public async Task InitializeAsync()
    {
        try
        {
            await _container.StartAsync();
        }
        catch (Exception exception)
        {
            // Fail loudly: a silently skipped capture test would look like passing evidence.
            throw new InvalidOperationException(
                $"Email capture tests need a running Docker daemon (Testcontainers could not start '{Image}'). " +
                "Start Docker and re-run; these tests are intentionally not skipped.",
                exception);
        }
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    /// <summary>Returns the captured message addressed to <paramref name="recipient"/> (each test uses its own recipient).</summary>
    public async Task<CapturedMessage> GetMessageToAsync(string recipient)
    {
        using var api = new HttpClient { BaseAddress = new Uri($"http://{_container.Hostname}:{_container.GetMappedPublicPort(HttpPort)}") };
        var search = await api.GetFromJsonAsync<SearchResult>($"/api/v1/search?query={Uri.EscapeDataString($"to:{recipient}")}");
        var summary = Assert.Single(search!.Messages);
        return (await api.GetFromJsonAsync<CapturedMessage>($"/api/v1/message/{summary.Id}"))!;
    }

    private sealed record SearchResult([property: JsonPropertyName("messages")] List<Summary> Messages);

    private sealed record Summary([property: JsonPropertyName("ID")] string Id);
}

public sealed record CapturedMessage(
    [property: JsonPropertyName("Subject")] string Subject,
    [property: JsonPropertyName("Text")] string Text,
    [property: JsonPropertyName("HTML")] string Html,
    [property: JsonPropertyName("From")] CapturedAddress From,
    [property: JsonPropertyName("To")] List<CapturedAddress> To);

public sealed record CapturedAddress(
    [property: JsonPropertyName("Name")] string Name,
    [property: JsonPropertyName("Address")] string Address);

[CollectionDefinition(Name)]
public sealed class MailpitTestGroup : ICollectionFixture<MailpitFixture>
{
    public const string Name = "mailpit";
}
