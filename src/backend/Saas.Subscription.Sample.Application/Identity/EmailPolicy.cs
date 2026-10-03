namespace Saas.Subscription.Sample.Application.Identity;

public enum EmailPolicyResult
{
    Valid,
    Empty,
    TooLong,
    InvalidFormat,
}

/// <summary>
/// Bounds and a deliberately plain shape check for the email a person may submit: one <c>@</c>, a non-empty
/// local part, a dotted domain, no whitespace or control characters. Whether the address is real or reachable
/// is proven by email verification (P06), not by a stricter regular expression.
/// </summary>
public static class EmailPolicy
{
    /// <summary>RFC 5321 path limit; the column allows 320.</summary>
    public const int MaxLength = 254;

    public const int MaxLocalPartLength = 64;

    public static EmailPolicyResult Check(string? email)
    {
        var value = email?.Trim();
        if (string.IsNullOrEmpty(value))
        {
            return EmailPolicyResult.Empty;
        }

        if (value.Length > MaxLength)
        {
            return EmailPolicyResult.TooLong;
        }

        var at = value.IndexOf('@');
        if (at <= 0 || at != value.LastIndexOf('@') || at > MaxLocalPartLength)
        {
            return EmailPolicyResult.InvalidFormat;
        }

        if (value.Any(character => char.IsWhiteSpace(character) || char.IsControl(character)))
        {
            return EmailPolicyResult.InvalidFormat;
        }

        var domain = value[(at + 1)..];
        var hasValidDomain = domain.Contains('.', StringComparison.Ordinal)
            && !domain.StartsWith('.') && !domain.EndsWith('.')
            && !domain.Contains("..", StringComparison.Ordinal);

        return hasValidDomain ? EmailPolicyResult.Valid : EmailPolicyResult.InvalidFormat;
    }
}
