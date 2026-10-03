using Microsoft.EntityFrameworkCore;
using Saas.Subscription.Sample.Application.Identity;
using Saas.Subscription.Sample.Domain.Identity;
using Saas.Subscription.Sample.Infrastructure.Persistence;

namespace Saas.Subscription.Sample.Infrastructure.Identity;

public sealed class UserProfileReader(AppDbContext db) : IUserProfileReader
{
    public Task<UserProfile?> FindActiveAsync(Guid userId, CancellationToken cancellationToken) =>
        db.Users.AsNoTracking()
            .Where(user => user.Id == userId && user.Status == UserStatus.Active)
            .Select(user => new UserProfile(user.Id, user.Email, user.CreatedAt))
            .SingleOrDefaultAsync(cancellationToken);
}
