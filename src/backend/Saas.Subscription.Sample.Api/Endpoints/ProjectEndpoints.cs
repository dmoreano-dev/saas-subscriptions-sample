using Saas.Subscription.Sample.Api.Contracts.Projects;
using Saas.Subscription.Sample.Api.Problems;

namespace Saas.Subscription.Sample.Api.Endpoints;

public static class ProjectEndpoints
{
    public static void MapProjectEndpoints(this RouteGroupBuilder api)
    {
        // accountId is only a selector, never proof of membership. A foreign account or project is 404, not 403.
        var projects = api.MapGroup("/accounts/{accountId:guid}/projects")
            .WithTags("Projects")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        projects.MapGet("/", (Guid accountId) => ApiProblems.NotImplemented())
            .WithName("ListProjects")
            .Produces<ProjectListResponse>()
            .ProducesProblem(StatusCodes.Status403Forbidden);

        projects.MapPost("/", (Guid accountId, CreateProjectRequest request) => ApiProblems.NotImplemented())
            .WithName("CreateProject")
            .WithSummary("403 capability_not_granted when the plan has no projects; 409 quota_exceeded at the project limit.")
            .Produces<ProjectDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);

        projects.MapGet("/{projectId:guid}", (Guid accountId, Guid projectId) => ApiProblems.NotImplemented())
            .WithName("GetProject")
            .Produces<ProjectDto>()
            .ProducesProblem(StatusCodes.Status403Forbidden);

        projects.MapDelete("/{projectId:guid}", (Guid accountId, Guid projectId) => ApiProblems.NotImplemented())
            .WithName("DeleteProject")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }
}
