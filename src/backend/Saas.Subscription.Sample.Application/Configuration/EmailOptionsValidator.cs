using Microsoft.Extensions.Options;

namespace Saas.Subscription.Sample.Application.Configuration;

public sealed class EmailOptionsValidator(AppEnvironment environment) : IValidateOptions<EmailOptions>
{
    private const int MaxPort = 65535;

    private const string ProviderPath = $"{EmailOptions.SectionName}:{nameof(EmailOptions.Provider)}";

    public ValidateOptionsResult Validate(string? name, EmailOptions options)
    {
        var failures = new List<string>();

        switch (options.Provider)
        {
            case EmailProvider.Smtp:
                if (environment.IsHosted)
                {
                    // Hosts such as Render block outbound SMTP, and a local capture service is not delivery.
                    failures.Add($"{ProviderPath} must be Https when hosted.");
                }

                if (OptionsValidation.IsBlank(options.Smtp.Host))
                {
                    failures.Add($"{EmailOptions.SectionName}:{nameof(EmailOptions.Smtp)}:{nameof(SmtpOptions.Host)} is required when {ProviderPath} is Smtp.");
                }

                if (options.Smtp.Port is < 1 or > MaxPort)
                {
                    failures.Add($"{EmailOptions.SectionName}:{nameof(EmailOptions.Smtp)}:{nameof(SmtpOptions.Port)} must be between 1 and {MaxPort}.");
                }

                break;

            case EmailProvider.Https:
                if (OptionsValidation.IsBlank(options.Https.ApiKey))
                {
                    failures.Add($"{EmailOptions.SectionName}:{nameof(EmailOptions.Https)}:{nameof(HttpsEmailOptions.ApiKey)} is required when {ProviderPath} is Https.");
                }

                break;
        }

        return failures.ToResult();
    }
}
