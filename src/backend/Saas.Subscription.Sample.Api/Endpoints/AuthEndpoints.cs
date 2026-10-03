using Saas.Subscription.Sample.Api.Contracts.Accounts;
using Saas.Subscription.Sample.Api.Contracts.Auth;
using Saas.Subscription.Sample.Api.Contracts.Users;
using Saas.Subscription.Sample.Api.Problems;
using Saas.Subscription.Sample.Application.Identity;

namespace Saas.Subscription.Sample.Api.Endpoints;

public static class AuthEndpoints
{
    private const string BearerTokenType = "Bearer";
    private const string NoStore = "no-store";

    public static void MapAuthEndpoints(this RouteGroupBuilder api)
    {
        var auth = api.MapGroup("/auth")
            .WithTags("Auth")
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        // A duplicate email gets the same 201 as a new one (ADR 0008): the only visible failures are field
        // validation errors, which say nothing about which emails exist.
        auth.MapPost("/register", RegisterAsync)
            .WithName("Register")
            .WithSummary("Creates a user and their personal account. Does not sign the user in.")
            .Produces<RegisterResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        auth.MapPost("/login", LoginAsync)
            .WithName("Login")
            .WithSummary("Exchanges credentials for a short-lived access token. Failures are always the generic invalid_credentials.")
            .Produces<LoginResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }

    private static async Task<IResult> RegisterAsync(
        RegisterRequest request,
        IRegistrationService registration,
        CancellationToken cancellationToken)
    {
        var errors = ValidateRegistration(request);
        if (errors.Count > 0)
        {
            return ApiProblems.Validation(errors);
        }

        var result = await registration.RegisterAsync(request.Email.Trim(), request.Password, cancellationToken);
        var registered = result.User;

        var response = new RegisterResponse(
            new UserDto(registered.UserId, registered.Email, registered.CreatedAt),
            new AccountSummary(registered.PersonalAccountId, registered.PersonalAccountName, AccountType.Personal, AccountStatus.Active));
        return Results.Json(response, statusCode: StatusCodes.Status201Created);
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        ILoginService login,
        IAccessTokenIssuer tokens,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var errors = ValidateLogin(request);
        if (errors.Count > 0)
        {
            return ApiProblems.Validation(errors);
        }

        var result = await login.LoginAsync(request.Email, request.Password, cancellationToken);
        if (result.Outcome == LoginOutcome.Throttled)
        {
            return ApiProblems.RateLimited(result.RetryAfter ?? TimeSpan.Zero);
        }

        if (result.Outcome != LoginOutcome.Succeeded || result.Identity is null)
        {
            return ApiProblems.Unauthenticated(ProblemCodes.InvalidCredentials);
        }

        var identity = result.Identity;
        var token = tokens.Issue(identity.UserId);

        // A token is a bearer secret: no cache between the API and the browser may keep this response.
        httpContext.Response.Headers.CacheControl = NoStore;

        return Results.Ok(new LoginResponse(
            token.Value,
            BearerTokenType,
            token.ExpiresAt,
            new UserDto(identity.UserId, identity.Email, identity.CreatedAt)));
    }

    /// <summary>
    /// Only "was something sent": what the email or password look like is not checked here, so a malformed value
    /// gets the same generic <c>invalid_credentials</c> as a wrong one.
    /// </summary>
    private static Dictionary<string, string[]> ValidateLogin(LoginRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            errors["email"] = ["Email is required."];
        }

        if (string.IsNullOrEmpty(request.Password))
        {
            errors["password"] = ["Password is required."];
        }

        return errors;
    }

    private static Dictionary<string, string[]> ValidateRegistration(RegisterRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        var email = EmailPolicy.Check(request.Email) switch
        {
            EmailPolicyResult.Valid => null,
            EmailPolicyResult.Empty => "Email is required.",
            EmailPolicyResult.TooLong => $"Email must be at most {EmailPolicy.MaxLength} characters.",
            _ => "Email must be a valid address.",
        };
        if (email is not null)
        {
            errors["email"] = [email];
        }

        var password = PasswordPolicy.Check(request.Password) switch
        {
            PasswordPolicyResult.Valid => null,
            _ => $"Password must be between {PasswordPolicy.MinLength} and {PasswordPolicy.MaxLength} characters.",
        };
        if (password is not null)
        {
            errors["password"] = [password];
        }

        return errors;
    }
}
