using Saas.Subscription.Sample.Domain.Identity;

namespace Saas.Subscription.Sample.Application.Identity;

public interface IPasswordHasher
{
    StoredPasswordHash Hash(string password);

    PasswordVerificationResult Verify(string password, StoredPasswordHash stored);
}
