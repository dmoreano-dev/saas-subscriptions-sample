using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Saas.Subscription.Sample.Api.Endpoints;
using Saas.Subscription.Sample.Application.Common;
using Saas.Subscription.Sample.Application.Email;
using Saas.Subscription.Sample.Infrastructure.Email;
using Saas.Subscription.Sample.IntegrationTests.Configuration;

namespace Saas.Subscription.Sample.IntegrationTests.Email;

/// <summary>Which <see cref="IEmailSender"/> each configuration resolves, and the Development-only test endpoint. No Docker needed.</summary>
public class EmailSenderRegistrationTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private const string Recipient = "dev@example.test";

    [Fact]
    public void Resolve_DevelopmentDefaults_IsTheSmtpSender()
    {
        // Act
        var sender = factory.Services.GetRequiredService<IEmailSender>();

        // Assert
        Assert.IsType<SmtpEmailSender>(sender);
    }

    [Fact]
    public async Task SendAsync_HttpsProviderWithoutAdapter_ThrowsDependencyUnavailableButHostStarts()
    {
        // Arrange
        using var hosted = HostedProfile.Create(factory);
        var sender = hosted.Services.GetRequiredService<IEmailSender>();
        var message = new EmailMessage(Recipient, "subject", "<p>html</p>", "text");

        // Act
        var act = () => sender.SendAsync(message, CancellationToken.None);

        // Assert
        Assert.IsType<UnavailableEmailSender>(sender);
        await Assert.ThrowsAsync<DependencyUnavailableException>(act);
    }

    [Fact]
    public async Task PostTestEmail_InDevelopment_SendsOneHtmlAndTextMessage()
    {
        // Arrange
        var sender = new RecordingEmailSender();
        using var development = factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services => services.AddSingleton<IEmailSender>(sender)));
        using var client = development.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(DevEndpoints.TestEmailRoute, new { to = Recipient });

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var sent = Assert.Single(sender.Sent);
        Assert.Equal(Recipient, sent.To);
        Assert.NotEmpty(sent.HtmlBody);
        Assert.NotEmpty(sent.TextBody);
    }

    [Fact]
    public async Task PostTestEmail_InvalidRecipient_ReturnsBadRequestAndSendsNothing()
    {
        // Arrange
        var sender = new RecordingEmailSender();
        using var development = factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services => services.AddSingleton<IEmailSender>(sender)));
        using var client = development.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(DevEndpoints.TestEmailRoute, new { to = "not-an-email" });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(sender.Sent);
    }

    [Fact]
    public async Task PostTestEmail_Hosted_IsNotMapped()
    {
        // Arrange
        using var hosted = HostedProfile.Create(factory);
        using var client = hosted.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(DevEndpoints.TestEmailRoute, new { to = Recipient });

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private sealed class RecordingEmailSender : IEmailSender
    {
        public List<EmailMessage> Sent { get; } = [];

        public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
        {
            Sent.Add(message);
            return Task.CompletedTask;
        }
    }
}
