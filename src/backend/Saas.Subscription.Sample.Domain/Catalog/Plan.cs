using Saas.Subscription.Sample.Domain.Accounts;

namespace Saas.Subscription.Sample.Domain.Catalog;

/// <summary>
/// A commercial plan identity. <see cref="IsBillable"/> is false for Free: it has no prices, and no
/// customer or subscription at any payment provider is ever needed for it.
/// </summary>
public sealed class Plan
{
    private Plan()
    {
    }

    public Guid Id { get; private set; }

    public string Key { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    public AccountType AccountType { get; private set; }

    public bool IsBillable { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static Plan Create(string key, string name, AccountType accountType, bool isBillable, DateTimeOffset now) => new()
    {
        Id = Guid.CreateVersion7(now),
        Key = key,
        Name = name,
        AccountType = accountType,
        IsBillable = isBillable,
        CreatedAt = now,
    };
}
