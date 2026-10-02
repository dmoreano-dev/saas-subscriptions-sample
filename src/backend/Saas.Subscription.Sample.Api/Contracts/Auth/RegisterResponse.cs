using Saas.Subscription.Sample.Api.Contracts.Accounts;
using Saas.Subscription.Sample.Api.Contracts.Users;

namespace Saas.Subscription.Sample.Api.Contracts.Auth;

/// <summary>Registration creates the user and their personal account but does not sign them in (no token).</summary>
public sealed record RegisterResponse(UserDto User, AccountSummary PersonalAccount);
