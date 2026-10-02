using Saas.Subscription.Sample.Api.Contracts.Users;

namespace Saas.Subscription.Sample.Api.Contracts.Auth;

/// <summary>A short-lived access token to send as <c>Authorization: Bearer</c>; kept in memory by the client.</summary>
public sealed record LoginResponse(string AccessToken, string TokenType, DateTimeOffset ExpiresAt, UserDto User);
