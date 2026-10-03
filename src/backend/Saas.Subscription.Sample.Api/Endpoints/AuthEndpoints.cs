using Saas.Subscription.Sample.Api.Contracts.Accounts;
using Saas.Subscription.Sample.Api.Contracts.Auth;
using Saas.Subscription.Sample.Api.Contracts.Users;
using Saas.Subscription.Sample.Api.Problems;
using Saas.Subscription.Sample.Application.Identity;

namespace Saas.Subscription.Sample.Api.Endpoints;

public static class AuthEndpoints
{
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

        auth.MapPost("/login", (LoginRequest request) => ApiProblems.NotImplemented())
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
