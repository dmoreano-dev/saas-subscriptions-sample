using Microsoft.EntityFrameworkCore;
using Saas.Subscription.Sample.Application.Authorization;
using Saas.Subscription.Sample.Application.Projects;

namespace Saas.Subscription.Sample.Infrastructure.Persistence.Projects;

public sealed class ProjectStore(AppDbContext db) : IProjectStore
{
    public async Task<IReadOnlyList<ProjectView>> ListAsync(AccountScope scope, CancellationToken cancellationToken) =>
        await db.Projects.AsNoTracking()
            .Where(project => project.AccountId == scope.AccountId)
            .OrderBy(project => project.CreatedAt)
            .ThenBy(project => project.Id)
            .Select(project => new ProjectView(project.Id, project.AccountId, project.Name, project.Description, project.CreatedAt))
            .ToListAsync(cancellationToken);

    public Task<ProjectView?> FindAsync(AccountScope scope, Guid projectId, CancellationToken cancellationToken) =>
        db.Projects.AsNoTracking()
            .Where(project => project.AccountId == scope.AccountId && project.Id == projectId)
            .Select(project => new ProjectView(project.Id, project.AccountId, project.Name, project.Description, project.CreatedAt))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<bool> DeleteAsync(AccountScope scope, Guid projectId, CancellationToken cancellationToken)
    {
        // Raw SQL obeys the same boundary as the queries above: the account comes from the scope and is part of
        // the predicate, so the id of another account's project deletes nothing. Both values are parameters.
        var deleted = await db.Database.ExecuteSqlAsync(
            $"DELETE FROM projects WHERE account_id = {scope.AccountId} AND id = {projectId}",
            cancellationToken);

        return deleted == 1;
    }
}
