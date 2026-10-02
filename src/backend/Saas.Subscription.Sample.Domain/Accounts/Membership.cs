namespace Saas.Subscription.Sample.Domain.Accounts;

public sealed class Membership
{
    private Membership()
    {
    }

    public Guid Id { get; private set; }

    public Guid AccountId { get; private set; }

    public Guid UserId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static Membership Create(Guid accountId, Guid userId, DateTimeOffset now) => new()
    {
        Id = Guid.CreateVersion7(now),
        AccountId = accountId,
        UserId = userId,
        CreatedAt = now,
        UpdatedAt = now,
    };
}
