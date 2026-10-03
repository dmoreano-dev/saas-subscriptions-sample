using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Saas.Subscription.Sample.Application.Identity;

namespace Saas.Subscription.Sample.Infrastructure.Identity;

public static class IdentityServiceCollectionExtensions
{
    /// <summary>
    /// Registration, credential login, access-token issuance and the caller's profile. Needs <c>AddPasswordHashing</c>, <c>AddPersistence</c> and a
    /// <see cref="TimeProvider"/>. The throttle, the dummy hash and the signing keys are singletons: each holds state
    /// for the process.
    /// </summary>
    public static IServiceCollection AddIdentityServices(this IServiceCollection services)
    {
        services.TryAddSingleton(LoginThrottlePolicy.Default);
        services.TryAddSingleton<ILoginThrottle, InMemoryLoginThrottle>();
        services.TryAddSingleton<DummyPasswordHash>();
        services.AddScoped<IRegistrationService, RegistrationService>();
        services.AddScoped<ILoginService, LoginService>();
        services.AddScoped<IUserProfileReader, UserProfileReader>();
        services.TryAddSingleton<AccessTokenSigningKeys>();
        services.AddSingleton<IAccessTokenIssuer, JwtAccessTokenIssuer>();
        return services;
    }
}
