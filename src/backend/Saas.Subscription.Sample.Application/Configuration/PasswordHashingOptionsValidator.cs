using Microsoft.Extensions.Options;

namespace Saas.Subscription.Sample.Application.Configuration;

public sealed class PasswordHashingOptionsValidator : IValidateOptions<PasswordHashingOptions>
{
    public ValidateOptionsResult Validate(string? name, PasswordHashingOptions options)
    {
        var failures = new List<string>();

        if (options.MemoryMebibytes is < PasswordHashingOptions.MinMemoryMebibytes or > PasswordHashingOptions.MaxMemoryMebibytes)
        {
            failures.Add(
                $"{PasswordHashingOptions.SectionName}:{nameof(PasswordHashingOptions.MemoryMebibytes)} must be between {PasswordHashingOptions.MinMemoryMebibytes} and {PasswordHashingOptions.MaxMemoryMebibytes}.");
        }

        if (options.Iterations is < PasswordHashingOptions.MinIterations or > PasswordHashingOptions.MaxIterations)
        {
            failures.Add(
                $"{PasswordHashingOptions.SectionName}:{nameof(PasswordHashingOptions.Iterations)} must be between {PasswordHashingOptions.MinIterations} and {PasswordHashingOptions.MaxIterations}.");
        }

        return failures.ToResult();
    }
}
