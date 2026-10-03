using System.Text;

namespace Saas.Subscription.Sample.Application.Identity;

public enum PasswordPolicyResult
{
    Valid,
    TooShort,
    TooLong,
}

/// <summary>
/// Bounds on the password a person may submit. The lower bound is a lab value (NIST recommends at least 8);
/// the upper bound keeps the cost of one hash predictable so a huge input cannot be used to burn CPU.
/// No composition rules and no Unicode normalization (the bytes typed are the bytes hashed).
/// </summary>
public static class PasswordPolicy
{
    public const int MinLength = 5;
    public const int MaxLength = 50;

    /// <summary>Length is counted in Unicode characters, so an emoji is one, not two UTF-16 units.</summary>
    public static PasswordPolicyResult Check(string? password)
    {
        if (string.IsNullOrEmpty(password))
        {
            return PasswordPolicyResult.TooShort;
        }

        // An input far beyond the limit is rejected without counting it character by character.
        if (password.Length > MaxLength * 2)
        {
            return PasswordPolicyResult.TooLong;
        }

        var length = password.EnumerateRunes().Count();
        return length switch
        {
            < MinLength => PasswordPolicyResult.TooShort,
            > MaxLength => PasswordPolicyResult.TooLong,
            _ => PasswordPolicyResult.Valid,
        };
    }
}
