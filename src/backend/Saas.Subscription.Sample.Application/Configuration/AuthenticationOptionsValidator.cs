using Microsoft.Extensions.Options;

namespace Saas.Subscription.Sample.Application.Configuration;

public sealed class AuthenticationOptionsValidator(AppEnvironment environment) : IValidateOptions<AuthenticationOptions>
{
    public ValidateOptionsResult Validate(string? name, AuthenticationOptions options)
    {
        var failures = new List<string>();

        // Locally JWT-03 can use a user-secrets key; hosted must never run without persistent signing material.
        if (environment.IsHosted)
        {
            if (OptionsValidation.IsBlank(options.SigningKey))
            {
                failures.Add($"{AuthenticationOptions.SectionName}:{nameof(AuthenticationOptions.SigningKey)} is required when hosted.");
            }

            if (OptionsValidation.IsBlank(options.SigningKeyId))
            {
                failures.Add($"{AuthenticationOptions.SectionName}:{nameof(AuthenticationOptions.SigningKeyId)} is required when hosted.");
            }
        }

        return failures.ToResult();
    }
}
