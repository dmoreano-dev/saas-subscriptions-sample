using System.ComponentModel.DataAnnotations;
using Saas.Subscription.Sample.Application.Email;

namespace Saas.Subscription.Sample.Api.Endpoints;

/// <summary>
/// Local-only helpers, mapped only in Development and excluded from the OpenAPI contract: they are tooling,
/// not part of the product API.
/// </summary>
public static class DevEndpoints
{
    public const string TestEmailRoute = "/dev/email/test";

    public static IEndpointRouteBuilder MapDevEndpoints(this IEndpointRouteBuilder app)
    {
        // Sends one fixed synthetic message so a developer can inspect it in the local capture service.
        app.MapPost(TestEmailRoute, async (TestEmailRequest request, IEmailSender sender, CancellationToken cancellationToken) =>
            {
                if (!new EmailAddressAttribute().IsValid(request.To))
                {
                    return Results.ValidationProblem(new Dictionary<string, string[]> { [nameof(request.To)] = ["A valid email address is required."] });
                }

                await sender.SendAsync(TestMessage(request.To!), cancellationToken);
                return Results.Accepted();
            })
            .ExcludeFromDescription();

        return app;
    }

    private static EmailMessage TestMessage(string to) => new(
        to,
        "Local email capture test",
        "<h1>Local email capture test</h1><p>If you can read this in Mailpit, <strong>HTML</strong> rendering works.</p>",
        "Local email capture test\n\nIf you can read this in Mailpit, the plain-text part works.");

    public sealed record TestEmailRequest(string? To);
}
