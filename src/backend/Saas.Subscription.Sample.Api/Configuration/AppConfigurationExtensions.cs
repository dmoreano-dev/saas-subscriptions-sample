using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Saas.Subscription.Sample.Application.Configuration;

namespace Saas.Subscription.Sample.Api.Configuration;

public static class AppConfigurationExtensions
{
    /// <summary>
    /// Binds every configuration group to a typed, validated options class and registers the clock. Validation
    /// runs when the host starts, so an incomplete or unsafe configuration stops the API before it serves a request.
    /// Registration is lazy (nothing reads configuration here) so test-host overrides are honored.
    /// </summary>
    public static IServiceCollection AddAppConfiguration(this IServiceCollection services)
    {
        services.AddSingleton(provider =>
        {
            var host = provider.GetRequiredService<IHostEnvironment>();
            return new AppEnvironment(host.EnvironmentName, IsHosted: !host.IsDevelopment());
        });

        services.AddValidatedOptions<DatabaseOptions, DatabaseOptionsValidator>(DatabaseOptions.SectionName)
            .Configure<IConfiguration>((options, configuration) =>
                options.ConnectionString ??= configuration.GetConnectionString(DatabaseOptions.AspireConnectionName));
        services.AddValidatedOptions<AuthenticationOptions, AuthenticationOptionsValidator>(AuthenticationOptions.SectionName);
        services.AddValidatedOptions<BillingOptions, BillingOptionsValidator>(BillingOptions.SectionName);
        services.AddValidatedOptions<EmailOptions, EmailOptionsValidator>(EmailOptions.SectionName);
        services.AddValidatedOptions<CacheOptions, CacheOptionsValidator>(CacheOptions.SectionName);
        services.AddValidatedOptions<FrontendOptions, FrontendOptionsValidator>(FrontendOptions.SectionName);
        services.AddValidatedOptions<BackgroundWorkOptions, BackgroundWorkOptionsValidator>(BackgroundWorkOptions.SectionName);

        // The single source of business time; tests replace it with a FakeTimeProvider.
        services.TryAddSingleton(TimeProvider.System);

        return services;
    }

    private static OptionsBuilder<TOptions> AddValidatedOptions<TOptions, TValidator>(
        this IServiceCollection services,
        string sectionName)
        where TOptions : class
        where TValidator : class, IValidateOptions<TOptions>
    {
        services.AddSingleton<IValidateOptions<TOptions>, TValidator>();

        return services.AddOptions<TOptions>()
            .BindConfiguration(sectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
    }
}
