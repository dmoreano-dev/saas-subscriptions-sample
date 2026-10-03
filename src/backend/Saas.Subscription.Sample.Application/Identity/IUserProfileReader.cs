namespace Saas.Subscription.Sample.Application.Identity;

public interface IUserProfileReader
{
    /// <summary>The profile of an existing, active user; null when the user does not exist or is not active.</summary>
    Task<UserProfile?> FindActiveAsync(Guid userId, CancellationToken cancellationToken);
}

public sealed record UserProfile(Guid UserId, string Email, DateTimeOffset CreatedAt);
