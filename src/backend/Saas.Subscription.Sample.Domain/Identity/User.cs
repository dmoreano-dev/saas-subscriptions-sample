namespace Saas.Subscription.Sample.Domain.Identity;

/// <summary>
/// A global user
/// .</summary>
public sealed class User
{
    private User()
    {
    }

    public Guid Id { get; private set; }

    /// <summary>
    /// The address as typed by the person; used for display and for sending mail.
    /// </summary>
    public string Email { get; private set; } = null!;

    /// <summary>
    /// Unique, used for lookups.
    /// </summary>
    public string NormalizedEmail { get; private set; } = null!;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static User Create(string email, DateTimeOffset now) => new()
    {
        Id = Guid.CreateVersion7(now),
        Email = email.Trim(),
        NormalizedEmail = EmailNormalizer.Normalize(email),
        CreatedAt = now,
        UpdatedAt = now,
    };

    public void ChangeEmail(string email, DateTimeOffset now)
    {
        Email = email.Trim();
        NormalizedEmail = EmailNormalizer.Normalize(email);
        UpdatedAt = now;
    }
}
