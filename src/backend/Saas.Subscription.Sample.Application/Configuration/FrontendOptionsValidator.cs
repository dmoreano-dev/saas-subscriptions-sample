using Microsoft.Extensions.Options;

namespace Saas.Subscription.Sample.Application.Configuration;

public sealed class FrontendOptionsValidator(AppEnvironment environment) : IValidateOptions<FrontendOptions>
{
    private const string BaseUrlPath = $"{FrontendOptions.SectionName}:{nameof(FrontendOptions.BaseUrl)}";
    private const string AllowedOriginsPath = $"{FrontendOptions.SectionName}:{nameof(FrontendOptions.AllowedOrigins)}";

    public ValidateOptionsResult Validate(string? name, FrontendOptions options)
    {
        var failures = new List<string>();

        if (!OptionsValidation.IsBlank(options.BaseUrl))
        {
            ValidateOrigin(options.BaseUrl!, BaseUrlPath, failures);
        }

        foreach (var origin in options.AllowedOrigins)
        {
            ValidateOrigin(origin, AllowedOriginsPath, failures);
        }

        return failures.ToResult();
    }

    private void ValidateOrigin(string value, string path, List<string> failures)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)
            || uri.Scheme is not ("http" or "https")
            || uri.AbsolutePath != "/"
            || !string.IsNullOrEmpty(uri.Query)
            || !string.IsNullOrEmpty(uri.Fragment)
            || value.Contains('*', StringComparison.Ordinal))
        {
            failures.Add($"{path} must contain only absolute http(s) origins without a path, query, fragment or wildcard.");
            return;
        }

        if (environment.IsHosted && (uri.Scheme != Uri.UriSchemeHttps || OptionsValidation.IsLoopbackHost(uri.Host)))
        {
            failures.Add($"{path} must use https and a non-local host when hosted.");
        }
    }
}
