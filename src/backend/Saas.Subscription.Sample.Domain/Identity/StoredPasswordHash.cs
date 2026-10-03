namespace Saas.Subscription.Sample.Domain.Identity;

/// <summary>
/// Everything persisted about a password: never the password itself. <see cref="EncodedHash"/> is the
/// self-describing value (algorithm, work factor and salt travel inside it); <see cref="Algorithm"/> selects
/// the verifier without parsing it.
/// </summary>
public sealed record StoredPasswordHash(string Algorithm, string EncodedHash)
{
    /// <summary>The recoverable value must never reach logs or exception messages.</summary>
    public override string ToString() => $"{nameof(StoredPasswordHash)} {{ Algorithm = {Algorithm} }}";
}
