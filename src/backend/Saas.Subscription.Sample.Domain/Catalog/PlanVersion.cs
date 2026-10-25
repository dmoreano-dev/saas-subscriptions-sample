namespace Saas.Subscription.Sample.Domain.Catalog;

/// <summary>
/// An immutable commercial package once published. Changing a plan means a new version with the next
/// number; PostgreSQL triggers reject changes to a published version and to its capabilities and prices.
/// </summary>
public sealed class PlanVersion
{
    private readonly List<PlanCapability> _capabilities = [];
    private readonly List<PlanPrice> _prices = [];

    private PlanVersion()
    {
    }

    public Guid Id { get; private set; }

    public Guid PlanId { get; private set; }

    /// <summary>The plan this version belongs to; loaded only when a query includes it.</summary>
    public Plan Plan { get; private set; } = null!;

    public int Version { get; private set; }

    public PlanVersionStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? PublishedAt { get; private set; }

    public IReadOnlyCollection<PlanCapability> Capabilities => _capabilities;

    public IReadOnlyCollection<PlanPrice> Prices => _prices;

    public static PlanVersion CreateDraft(Guid planId, int version, DateTimeOffset now)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(version, 1);
        return new()
        {
            Id = Guid.CreateVersion7(now),
            PlanId = planId,
            Version = version,
            Status = PlanVersionStatus.Draft,
            CreatedAt = now,
        };
    }

    public void AddCapability(PlanCapability capability)
    {
        EnsureDraft();
        _capabilities.Add(capability);
    }

    public void AddPrice(PlanPrice price)
    {
        EnsureDraft();
        _prices.Add(price);
    }

    public void Publish(DateTimeOffset now)
    {
        EnsureDraft();
        Status = PlanVersionStatus.Published;
        PublishedAt = now;
    }

    private void EnsureDraft()
    {
        if (Status == PlanVersionStatus.Published)
        {
            throw new InvalidOperationException("A published plan version is immutable; create a new version instead.");
        }
    }
}
