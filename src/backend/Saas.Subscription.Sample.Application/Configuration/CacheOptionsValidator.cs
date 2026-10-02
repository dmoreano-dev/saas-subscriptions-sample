using Microsoft.Extensions.Options;

namespace Saas.Subscription.Sample.Application.Configuration;

public sealed class CacheOptionsValidator : IValidateOptions<CacheOptions>
{
    public ValidateOptionsResult Validate(string? name, CacheOptions options)
    {
        var failures = new List<string>();

        if (options.Provider == CacheProvider.Redis && OptionsValidation.IsBlank(options.Redis.ConnectionString))
        {
            failures.Add(
                $"{CacheOptions.SectionName}:{nameof(CacheOptions.Redis)}:{nameof(RedisOptions.ConnectionString)} is required when {CacheOptions.SectionName}:{nameof(CacheOptions.Provider)} is Redis.");
        }

        return failures.ToResult();
    }
}
