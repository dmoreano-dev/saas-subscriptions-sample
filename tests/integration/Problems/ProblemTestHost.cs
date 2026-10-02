using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Saas.Subscription.Sample.Api.Problems;
using Saas.Subscription.Sample.Application.Common;

namespace Saas.Subscription.Sample.IntegrationTests.Problems;

/// <summary>
/// A minimal host that uses the same problem-handling registration as the API plus one route per error
/// kind. No endpoint of the real API can fail in each of these ways yet, and none touches the database.
/// </summary>
public sealed class ProblemTestHost : IAsyncLifetime
{
    public const string SecretExceptionMessage = "SECRET-connection-string-Host=db.internal";

    public const string Unauthenticated = "/problems/unauthenticated";
    public const string Forbidden = "/problems/forbidden";
    public const string CapabilityNotGranted = "/problems/capability-not-granted";
    public const string NotFound = "/problems/not-found";
    public const string Conflict = "/problems/conflict";
    public const string QuotaExceeded = "/problems/quota-exceeded";
    public const string RateLimited = "/problems/rate-limited";
    public const string ServiceUnavailable = "/problems/service-unavailable";
    public const string BadRequest = "/problems/bad-request";
    public const string Validation = "/problems/validation";
    public const string BareUnauthorized = "/bare/401";
    public const string BareForbidden = "/bare/403";
    public const string BareNotFound = "/bare/404";
    public const string BareTooManyRequests = "/bare/429";
    public const string ThrowDependencyUnavailable = "/throw/dependency-unavailable";
    public const string ThrowUnexpected = "/throw/unexpected";
    public const string Ok = "/ok";

    public const int RetryAfterSeconds = 30;
    public const string ValidationField = "email";

    private WebApplication? _app;

    public HttpClient CreateClient() => _app!.GetTestClient();

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddApiProblemHandling();

        var app = builder.Build();
        app.UseApiProblemHandling();

        app.MapGet(Unauthenticated, () => ApiProblems.Unauthenticated());
        app.MapGet(Forbidden, () => ApiProblems.Forbidden());
        app.MapGet(CapabilityNotGranted, () => ApiProblems.Forbidden(ProblemCodes.CapabilityNotGranted));
        app.MapGet(NotFound, () => ApiProblems.NotFound());
        app.MapGet(Conflict, () => ApiProblems.Conflict());
        app.MapGet(QuotaExceeded, () => ApiProblems.QuotaExceeded());
        app.MapGet(RateLimited, () => ApiProblems.RateLimited(TimeSpan.FromSeconds(RetryAfterSeconds)));
        app.MapGet(ServiceUnavailable, () => ApiProblems.ServiceUnavailable());
        app.MapGet(BadRequest, () => ApiProblems.BadRequest());
        app.MapGet(Validation, () => ApiProblems.Validation(new Dictionary<string, string[]> { [ValidationField] = ["Invalid."] }));
        app.MapGet(BareUnauthorized, () => Results.StatusCode(StatusCodes.Status401Unauthorized));
        app.MapGet(BareForbidden, () => Results.StatusCode(StatusCodes.Status403Forbidden));
        app.MapGet(BareNotFound, () => Results.StatusCode(StatusCodes.Status404NotFound));
        app.MapGet(BareTooManyRequests, () => Results.StatusCode(StatusCodes.Status429TooManyRequests));
        app.MapGet(ThrowDependencyUnavailable, IResult () => throw new DependencyUnavailableException(SecretExceptionMessage));
        app.MapGet(ThrowUnexpected, IResult () => throw new InvalidOperationException(SecretExceptionMessage));
        app.MapGet(Ok, () => Results.Ok());

        await app.StartAsync();
        _app = app;
    }

    public async Task DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.DisposeAsync();
        }
    }
}
