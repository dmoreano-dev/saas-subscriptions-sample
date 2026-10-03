using Microsoft.Extensions.DependencyInjection;
using Saas.Subscription.Sample.Application.Identity;

namespace Saas.Subscription.Sample.Infrastructure.Identity;

public static class PasswordHashingServiceCollectionExtensions
{
    public static IServiceCollection AddPasswordHashing(this IServiceCollection services)
    {
        services.AddSingleton<IPasswordHasher, Argon2idPasswordHasher>();
        return services;
    }
}
