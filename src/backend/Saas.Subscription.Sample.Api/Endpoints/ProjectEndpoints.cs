using Saas.Subscription.Sample.Api.Authorization;
using Saas.Subscription.Sample.Api.Contracts.Projects;
using Saas.Subscription.Sample.Api.Problems;
using Saas.Subscription.Sample.Application.Projects;
using Saas.Subscription.Sample.Domain.Authorization;

namespace Saas.Subscription.Sample.Api.Endpoints;

public static class ProjectEndpoints
{
    public static void MapProjectEndpoints(this RouteGroupBuilder api)
    {
        // accountId is only a selector, never proof of membership. A foreign account or project is 404, not 403.
        // Handlers bind accountId for the contract only and read the verified account from the scope.
        var projects = api.MapGroup("/accounts/{accountId:guid}/projects")
            .WithTags("Projects")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        projects.MapGet("/", ListAsync)
            .RequireAccountAccess(Permissions.ProjectsRead)
            .WithName("ListProjects")
            .Produces<ProjectListResponse>()
            .ProducesProblem(StatusCodes.Status403Forbidden);

        // Creation needs the plan's project limit (ENT-04); until then it is authorized and answers 501.
        projects.MapPost("/", (Guid accountId, CreateProjectRequest request) => ApiProblems.NotImplemented())
            .RequireAccountAccess(Permissions.ProjectsWrite)
            .WithName("CreateProject")
            .WithSummary("403 capability_not_granted when the plan has no projects; 409 quota_exceeded at the project limit.")
            .Produces<ProjectDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);

        projects.MapGet("/{projectId:guid}", GetAsync)
            .RequireAccountAccess(Permissions.ProjectsRead)
            .WithName("GetProject")
            .Produces<ProjectDto>()
            .ProducesProblem(StatusCodes.Status403Forbidden);

        projects.MapDelete("/{projectId:guid}", DeleteAsync)
            .RequireAccountAccess(Permissions.ProjectsWrite)
            .WithName("DeleteProject")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }

    private static async Task<IResult> ListAsync(Guid accountId, HttpContext httpContext, IProjectStore projects, CancellationToken cancellationToken)
    {
        var items = await projects.ListAsync(httpContext.GetAccountScope(), cancellationToken);
        return Results.Ok(new ProjectListResponse([.. items.Select(ToDto)]));
    }

    private static async Task<IResult> GetAsync(Guid accountId, Guid projectId, HttpContext httpContext, IProjectStore projects, CancellationToken cancellationToken)
    {
        var project = await projects.FindAsync(httpContext.GetAccountScope(), projectId, cancellationToken);
        return project is null ? ApiProblems.NotFound() : Results.Ok(ToDto(project));
    }

    private static async Task<IResult> DeleteAsync(Guid accountId, Guid projectId, HttpContext httpContext, IProjectStore projects, CancellationToken cancellationToken)
    {
        var deleted = await projects.DeleteAsync(httpContext.GetAccountScope(), projectId, cancellationToken);
        return deleted ? Results.NoContent() : ApiProblems.NotFound();
    }

    private static ProjectDto ToDto(ProjectView project) =>
        new(project.Id, project.AccountId, project.Name, project.Description, project.CreatedAt);
}
