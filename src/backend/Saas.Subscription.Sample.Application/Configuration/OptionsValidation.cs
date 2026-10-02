using Microsoft.Extensions.Options;

namespace Saas.Subscription.Sample.Application.Configuration;

internal static class OptionsValidation
{
    public static ValidateOptionsResult ToResult(this List<string> failures) =>
        failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);

    public static bool IsBlank(string? value) => string.IsNullOrWhiteSpace(value);

    public static bool IsLoopbackHost(string host) =>
        host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
        || host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase)
        || host is "127.0.0.1" or "[::1]";
}
