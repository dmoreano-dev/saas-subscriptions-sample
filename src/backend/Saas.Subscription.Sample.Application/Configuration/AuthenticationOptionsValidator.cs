using Microsoft.Extensions.Options;
using Saas.Subscription.Sample.Application.Identity;

namespace Saas.Subscription.Sample.Application.Configuration;

public sealed class AuthenticationOptionsValidator(AppEnvironment environment) : IValidateOptions<AuthenticationOptions>
{
    private const string LocalSetupHint =
        " Locally, generate one once and keep it in user-secrets (see README, \"Signing key\").";

    public ValidateOptionsResult Validate(string? name, AuthenticationOptions options)
    {
        var failures = new List<string>();
        var hint = environment.IsHosted ? string.Empty : LocalSetupHint;

        // A missing key is never replaced by a generated one: every restart would invalidate all issued tokens.
        // The messages name the setting only; the key material is never part of a failure.
        if (OptionsValidation.IsBlank(options.SigningKey))
        {
            failures.Add($"{AuthenticationOptions.SectionName}:{nameof(AuthenticationOptions.SigningKey)} is required.{hint}");
        }
        else if (SigningKeyPem.TryImport(options.SigningKey, out var key))
        {
            key.Dispose();
        }
        else
        {
            failures.Add(
                $"{AuthenticationOptions.SectionName}:{nameof(AuthenticationOptions.SigningKey)} must be an unencrypted PEM private key on the NIST P-256 curve (ES256).");
        }

        if (OptionsValidation.IsBlank(options.SigningKeyId))
        {
            failures.Add($"{AuthenticationOptions.SectionName}:{nameof(AuthenticationOptions.SigningKeyId)} is required.{hint}");
        }

        return failures.ToResult();
    }
}
