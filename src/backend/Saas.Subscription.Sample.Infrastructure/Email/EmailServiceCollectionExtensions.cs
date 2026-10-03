using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Saas.Subscription.Sample.Application.Configuration;
using Saas.Subscription.Sample.Application.Email;

namespace Saas.Subscription.Sample.Infrastructure.Email;

public static class EmailServiceCollectionExtensions
{
    public static IServiceCollection AddEmail(this IServiceCollection services)
    {
        services.AddSingleton<IEmailSender>(provider =>
            provider.GetRequiredService<IOptions<EmailOptions>>().Value.Provider switch
            {
                EmailProvider.Smtp => ActivatorUtilities.CreateInstance<SmtpEmailSender>(provider),
                _ => new UnavailableEmailSender(),
            });

        return services;
    }
}
