using Microsoft.Extensions.Options;

namespace Saas.Subscription.Sample.IntegrationTests.Configuration;

public static class StartupFailure
{
    /// <summary>Returns every validation failure that stopped the host, or an empty string when it started.</summary>
    public static string Describe(Action startHost)
    {
        var exception = Record.Exception(startHost);

        while (exception is not null and not OptionsValidationException)
        {
            exception = exception.InnerException;
        }

        return exception is OptionsValidationException validation ? string.Join(" | ", validation.Failures) : string.Empty;
    }
}
