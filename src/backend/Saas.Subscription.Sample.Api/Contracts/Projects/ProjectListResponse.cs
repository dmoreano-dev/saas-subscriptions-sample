namespace Saas.Subscription.Sample.Api.Contracts.Projects;

public sealed record ProjectListResponse(IReadOnlyList<ProjectDto> Items);
