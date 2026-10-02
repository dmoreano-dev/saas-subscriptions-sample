namespace Saas.Subscription.Sample.Domain.Accounts;

/// <summary>
/// The tenant boundary. A personal account belongs to exactly one user (<see cref="PersonalOwnerUserId"/>);
/// an organization account has no personal owner and is managed through memberships.
/// </summary>
public sealed class Account
{
    private Account()
    {
    }

    public Guid Id { get; private set; }

    public AccountType Type { get; private set; }

    public AccountStatus Status { get; private set; }

    public string Name { get; private set; } = null!;

    public Guid? PersonalOwnerUserId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static Account CreatePersonal(Guid ownerUserId, string name, DateTimeOffset now) => new()
    {
        Id = Guid.CreateVersion7(now),
        Type = AccountType.Personal,
        Status = AccountStatus.Active,
        Name = name,
        PersonalOwnerUserId = ownerUserId,
        CreatedAt = now,
        UpdatedAt = now,
    };

    public static Account CreateOrganization(string name, DateTimeOffset now) => new()
    {
        Id = Guid.CreateVersion7(now),
        Type = AccountType.Organization,
        Status = AccountStatus.Active,
        Name = name,
        CreatedAt = now,
        UpdatedAt = now,
    };
}
