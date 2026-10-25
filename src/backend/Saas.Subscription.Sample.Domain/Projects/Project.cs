namespace Saas.Subscription.Sample.Domain.Projects;

/// <summary>A business resource owned by exactly one account; the owner never changes.</summary>
public sealed class Project
{
    private Project()
    {
    }

    public Guid Id { get; private set; }

    public Guid AccountId { get; private set; }

    public string Name { get; private set; } = null!;

    public string? Description { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static Project Create(Guid accountId, string name, string? description, DateTimeOffset now) => new()
    {
        Id = Guid.CreateVersion7(now),
        AccountId = accountId,
        Name = name,
        Description = description,
        CreatedAt = now,
        UpdatedAt = now,
    };
}
