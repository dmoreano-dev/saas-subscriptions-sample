namespace Saas.Subscription.Sample.Api.Contracts.Users;

/// <summary>The caller's own profile. Never carries credentials, hashes, tokens or security stamps.</summary>
public sealed record UserDto(Guid Id, string Email, DateTimeOffset CreatedAt);
