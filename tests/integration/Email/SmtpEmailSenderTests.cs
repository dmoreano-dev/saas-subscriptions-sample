using Microsoft.Extensions.Options;
using Saas.Subscription.Sample.Application.Common;
using Saas.Subscription.Sample.Application.Configuration;
using Saas.Subscription.Sample.Application.Email;
using Saas.Subscription.Sample.Infrastructure.Email;

namespace Saas.Subscription.Sample.IntegrationTests.Email;

[Collection(MailpitTestGroup.Name)]
public class SmtpEmailSenderTests(MailpitFixture mailpit)
{
    private const string FromAddress = "no-reply@saas-sample.invalid";
    private const string FromName = "Subscription Lab";
    private const string Subject = "Synthetic message";
    private const string HtmlBody = "<p>Hello <strong>HTML</strong></p>";
    private const string TextBody = "Hello plain text";
    private const string UnusedPort = "1";

    [Fact]
    public async Task SendAsync_HtmlAndTextMessage_IsCapturedWithBothPartsAndConfiguredSender()
    {
        // Arrange
        const string recipient = "both-parts@example.test";
        var sender = CreateSender(mailpit.SmtpHost, mailpit.SmtpHostPort);
        var message = new EmailMessage(recipient, Subject, HtmlBody, TextBody);

        // Act
        await sender.SendAsync(message, CancellationToken.None);

        // Assert
        var captured = await mailpit.GetMessageToAsync(recipient);
        Assert.Equal(Subject, captured.Subject);
        Assert.Contains("Hello plain text", captured.Text, StringComparison.Ordinal);
        Assert.Contains(HtmlBody, captured.Html, StringComparison.Ordinal);
        Assert.Equal(FromAddress, captured.From.Address);
        Assert.Equal(FromName, captured.From.Name);
        Assert.Equal(recipient, Assert.Single(captured.To).Address);
    }

    [Fact]
    public async Task SendAsync_NothingListening_ThrowsDependencyUnavailable()
    {
        // Arrange
        var sender = CreateSender("127.0.0.1", int.Parse(UnusedPort, System.Globalization.CultureInfo.InvariantCulture));
        var message = new EmailMessage("unreachable@example.test", Subject, HtmlBody, TextBody);

        // Act
        var act = () => sender.SendAsync(message, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<DependencyUnavailableException>(act);
    }

    private static SmtpEmailSender CreateSender(string host, int port) =>
        new(Options.Create(new EmailOptions
        {
            Provider = EmailProvider.Smtp,
            FromAddress = FromAddress,
            FromName = FromName,
            Smtp = new SmtpOptions { Host = host, Port = port },
        }));
}
