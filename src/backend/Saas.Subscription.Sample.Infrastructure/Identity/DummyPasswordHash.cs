using System.Security.Cryptography;
using Saas.Subscription.Sample.Application.Identity;
using Saas.Subscription.Sample.Domain.Identity;

namespace Saas.Subscription.Sample.Infrastructure.Identity;

/// <summary>
/// A hash of a random password nobody knows, created once with the current work factor. Verifying against it
/// when a login has no real credential makes that attempt cost what a real one costs, so response time does not
/// reveal which emails exist. It can never verify successfully.
/// </summary>
public sealed class DummyPasswordHash(IPasswordHasher hasher)
{
    private const int RandomBytes = 24;

    private readonly Lazy<StoredPasswordHash> _hash = new(() =>
        hasher.Hash(Convert.ToBase64String(RandomNumberGenerator.GetBytes(RandomBytes))));

    public StoredPasswordHash Value => _hash.Value;
}
