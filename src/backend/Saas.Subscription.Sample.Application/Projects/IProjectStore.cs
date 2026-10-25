using Saas.Subscription.Sample.Application.Authorization;

namespace Saas.Subscription.Sample.Application.Projects;

/// <summary>
/// Projects of one verified account. Every operation takes the <see cref="AccountScope"/>, so a project id alone
/// never selects a row: a project of another account is indistinguishable from one that does not exist.
/// </summary>
public interface IProjectStore
{
    Task<IReadOnlyList<ProjectView>> ListAsync(AccountScope scope, CancellationToken cancellationToken);

    Task<ProjectView?> FindAsync(AccountScope scope, Guid projectId, CancellationToken cancellationToken);

    /// <summary>False when the account has no such project.</summary>
    Task<bool> DeleteAsync(AccountScope scope, Guid projectId, CancellationToken cancellationToken);
}

public sealed record ProjectView(Guid Id, Guid AccountId, string Name, string? Description, DateTimeOffset CreatedAt);
