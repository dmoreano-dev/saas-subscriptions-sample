using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using Saas.Subscription.Sample.Application.Common;
using Saas.Subscription.Sample.Application.Configuration;
using Saas.Subscription.Sample.Application.Email;

namespace Saas.Subscription.Sample.Infrastructure.Email;

/// <summary>
/// Plain SMTP to a local capture service (Mailpit; MailDev works the same way).
/// </summary>
public sealed class SmtpEmailSender(IOptions<EmailOptions> options) : IEmailSender
{
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var mime = EmailMimeBuilder.Build(settings, message);

        // Smtp:Host is required when the provider is Smtp (validated when the host starts).
        using var client = new SmtpClient();
        try
        {
            await client.ConnectAsync(settings.Smtp.Host!, settings.Smtp.Port, SecureSocketOptions.None, cancellationToken);
            await client.SendAsync(mime, cancellationToken);
            await client.DisconnectAsync(quit: true, cancellationToken);
        }
        catch (Exception exception) when (exception is SmtpCommandException or SmtpProtocolException or IOException or System.Net.Sockets.SocketException)
        {
            throw new DependencyUnavailableException("The SMTP server could not accept the message.", exception);
        }
    }
}
