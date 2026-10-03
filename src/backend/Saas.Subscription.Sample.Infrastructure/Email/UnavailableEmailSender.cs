using Saas.Subscription.Sample.Application.Common;
using Saas.Subscription.Sample.Application.Email;

namespace Saas.Subscription.Sample.Infrastructure.Email;

public sealed class UnavailableEmailSender : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken) =>
        throw new DependencyUnavailableException(
            "Email:Provider is Https but the hosted email adapter is not implemented yet (DEP-04).");
}
