using System.ComponentModel.DataAnnotations;

namespace Saas.Subscription.Sample.Application.Configuration;

public enum EmailProvider
{
    /// <summary>Local capture (Mailpit; MailDev can replace it). Not a delivery path.</summary>
    Smtp,

    /// <summary>Hosted HTTPS email API (Resend is the initial candidate).</summary>
    Https,
}

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public EmailProvider Provider { get; set; } = EmailProvider.Smtp;

    [Required]
    [EmailAddress]
    public string? FromAddress { get; set; }

    [Required]
    public string? FromName { get; set; }

    public SmtpOptions Smtp { get; set; } = new();

    public HttpsEmailOptions Https { get; set; } = new();
}

public sealed class SmtpOptions
{
    public string? Host { get; set; }

    public int Port { get; set; } = 1025;
}

public sealed class HttpsEmailOptions
{
    /// <summary>Secret.</summary>
    public string? ApiKey { get; set; }
}
