namespace Saas.Subscription.Sample.Application.Email;

/// <summary>
/// The seam every application notification goes through. Callers never know whether the message reaches a local
/// capture service (Mailpit/MailDev over SMTP) or a hosted provider; the sender comes from <c>EmailOptions</c>.
/// </summary>
public interface IEmailSender
{
    /// <summary>
    /// Sends one message. Throws <see cref="Common.DependencyUnavailableException"/> when the transport is unavailable.
    /// </summary>
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}

public sealed record EmailMessage(string To, string Subject, string HtmlBody, string TextBody);
