using System.Data.Common;
using Microsoft.Extensions.Options;

namespace Saas.Subscription.Sample.Application.Configuration;

public sealed class DatabaseOptionsValidator(AppEnvironment environment) : IValidateOptions<DatabaseOptions>
{
    private static readonly string[] SslModeKeys = ["SSL Mode", "SslMode"];
    private const string VerifyFull = "VerifyFull";

    public ValidateOptionsResult Validate(string? name, DatabaseOptions options)
    {
        var failures = new List<string>();

        // Required-ness is checked by DataAnnotations; here only the hosted TLS rule on a present value.
        if (!OptionsValidation.IsBlank(options.ConnectionString) && environment.IsHosted)
        {
            ValidateHostedTls(options.ConnectionString!, failures);
        }

        return failures.ToResult();
    }

    private static void ValidateHostedTls(string connectionString, List<string> failures)
    {
        var builder = new DbConnectionStringBuilder();
        try
        {
            builder.ConnectionString = connectionString;
        }
        catch (ArgumentException)
        {
            // The text is deliberately not echoed: it contains the password.
            failures.Add($"{DatabaseOptions.SectionName}:{nameof(DatabaseOptions.ConnectionString)} is not a valid connection string.");
            return;
        }

        var sslMode = SslModeKeys
            .Select(key => builder.TryGetValue(key, out var value) ? value?.ToString() : null)
            .FirstOrDefault(value => value is not null);

        if (!string.Equals(sslMode?.Replace("-", string.Empty, StringComparison.Ordinal), VerifyFull, StringComparison.OrdinalIgnoreCase))
        {
            failures.Add(
                $"{DatabaseOptions.SectionName}:{nameof(DatabaseOptions.ConnectionString)} must set 'SSL Mode={VerifyFull}' when hosted.");
        }
    }
}
