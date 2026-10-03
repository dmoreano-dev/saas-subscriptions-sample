using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Saas.Subscription.Sample.Application.Identity;

namespace Saas.Subscription.Sample.Infrastructure.Identity;

public static class IdentityServiceCollectionExtensions
{
    /// <summary>
    /// Registration and credential login. Needs <c>AddPasswordHashing</c>, <c>AddPersistence</c> and a
    /// <see cref="TimeProvider"/>. The throttle and the dummy hash are singletons: both hold state for the process.
    /// </summary>
    public static IServiceCollection AddIdentityServices(this IServiceCollection services)
    {
        services.TryAddSingleton(LoginThrottlePolicy.Default);
        services.TryAddSingleton<ILoginThrottle, InMemoryLoginThrottle>();
        services.TryAddSingleton<DummyPasswordHash>();
        services.AddScoped<IRegistrationService, RegistrationService>();
        services.AddScoped<ILoginService, LoginService>();
        return services;
    }
}
