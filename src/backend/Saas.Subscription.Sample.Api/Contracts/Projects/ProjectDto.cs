namespace Saas.Subscription.Sample.Api.Contracts.Projects;

/// <summary>
/// A project of one account. <see cref="AccountId"/> lets the client discard a response that arrives after
/// the user switched accounts.
/// </summary>
public sealed record ProjectDto(Guid Id, Guid AccountId, string Name, string? Description, DateTimeOffset CreatedAt);
