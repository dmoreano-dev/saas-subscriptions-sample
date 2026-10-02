using Microsoft.Extensions.Options;

namespace Saas.Subscription.Sample.Application.Configuration;

public sealed class BackgroundWorkOptionsValidator : IValidateOptions<BackgroundWorkOptions>
{
    public ValidateOptionsResult Validate(string? name, BackgroundWorkOptions options)
    {
        var failures = new List<string>();

        if (options.LeaseDuration <= options.PollInterval)
        {
            failures.Add(
                $"{BackgroundWorkOptions.SectionName}:{nameof(BackgroundWorkOptions.LeaseDuration)} must be longer than {BackgroundWorkOptions.SectionName}:{nameof(BackgroundWorkOptions.PollInterval)}.");
        }

        return failures.ToResult();
    }
}
