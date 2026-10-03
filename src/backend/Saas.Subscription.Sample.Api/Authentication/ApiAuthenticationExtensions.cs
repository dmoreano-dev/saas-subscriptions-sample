using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Saas.Subscription.Sample.Infrastructure.Identity;
using AuthenticationOptions = Saas.Subscription.Sample.Application.Configuration.AuthenticationOptions;

namespace Saas.Subscription.Sample.Api.Authentication;

public static class ApiAuthenticationExtensions
{
    private const string MissingRequiredClaims = "The token has no usable subject or token id.";

    /// <summary>
    /// Bearer authentication with the access tokens this API issues. A request without a valid token gets a bare
    /// 401 that the status-code pages turn into the <c>unauthenticated</c> problem response.
    /// </summary>
    public static IServiceCollection AddApiAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<AuthenticationOptions>, AccessTokenSigningKeys, TimeProvider>((bearer, authentication, keys, time) =>
            {
                bearer.TokenValidationParameters = AccessTokenValidation.CreateParameters(authentication.Value, keys, time);

                // Keep the JWT claim names (`sub`, `jti`) instead of rewriting them to the legacy XML-SOAP claim URIs.
                bearer.MapInboundClaims = false;

                // The 401 carries a bare `WWW-Authenticate: Bearer`; why a token was refused (expired, wrong audience, ...) is not told.
                bearer.IncludeErrorDetails = false;

                bearer.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        if (context.Principal is null || !context.Principal.HasRequiredAccessTokenClaims())
                        {
                            context.Fail(MissingRequiredClaims);
                        }

                        return Task.CompletedTask;
                    },
                };
            });

        services.AddAuthorization();

        return services;
    }
}
