using MimeKit;
using Saas.Subscription.Sample.Application.Configuration;
using Saas.Subscription.Sample.Application.Email;

namespace Saas.Subscription.Sample.Infrastructure.Email;

internal static class EmailMimeBuilder
{
    public static MimeMessage Build(EmailOptions options, EmailMessage message)
    {
        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(options.FromName, options.FromAddress!));
        mime.To.Add(MailboxAddress.Parse(message.To));
        mime.Subject = message.Subject;
        mime.Body = new BodyBuilder
        {
            TextBody = message.TextBody,
            HtmlBody = message.HtmlBody,
        }.ToMessageBody();

        return mime;
    }
}
