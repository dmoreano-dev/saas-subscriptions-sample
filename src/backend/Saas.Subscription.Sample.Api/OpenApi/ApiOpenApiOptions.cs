using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using Saas.Subscription.Sample.Api.Problems;

namespace Saas.Subscription.Sample.Api.OpenApi;

public static class ApiOpenApiOptions
{
    public const string DocumentName = "v1";

    private const string Description =
        "Contract of the Subscription Lab API. Until the phase that implements an endpoint, it responds " +
        "501 with the problem code `not_implemented`. Every error is an RFC 9457 problem response whose " +
        "`code` is stable and machine-readable and whose `correlationId` identifies the request in the logs.";

    public static void Configure(OpenApiOptions options)
    {
        options.AddDocumentTransformer((document, _, _) =>
        {
            document.Info = new OpenApiInfo
            {
                Title = "Subscription Lab API",
                Version = DocumentName,
                Description = Description,
            };

            // The host differs per environment; a server list would make the committed document environment-specific.
            document.Servers = [];

            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
            document.Components.SecuritySchemes[BearerAuthenticationRequirement.SchemeName] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
            };

            return Task.CompletedTask;
        });

        options.AddOperationTransformer((operation, context, _) =>
        {
            var requiresBearer = context.Description.ActionDescriptor.EndpointMetadata
                .OfType<BearerAuthenticationRequirement>()
                .Any();

            if (requiresBearer)
            {
                operation.Security =
                [
                    new OpenApiSecurityRequirement
                    {
                        [new OpenApiSecuritySchemeReference(BearerAuthenticationRequirement.SchemeName, context.Document)] = [],
                    },
                ];
            }

            return Task.CompletedTask;
        });

        options.AddSchemaTransformer((schema, context, _) =>
        {
            if (context.JsonTypeInfo.Type.IsEnum)
            {
                schema.Type = JsonSchemaType.String;
            }

            return Task.CompletedTask;
        });

        options.AddSchemaTransformer((schema, context, _) =>
        {
            if (!typeof(ProblemDetails).IsAssignableFrom(context.JsonTypeInfo.Type))
            {
                return Task.CompletedTask;
            }

            schema.Properties ??= new Dictionary<string, IOpenApiSchema>();
            schema.Properties[ProblemDetailsMembers.Code] = new OpenApiSchema
            {
                Type = JsonSchemaType.String,
                Enum = ProblemCodes.All.Select(code => (System.Text.Json.Nodes.JsonNode)System.Text.Json.Nodes.JsonValue.Create(code)!).ToList(),
                Description = "Stable machine-readable error code.",
            };
            schema.Properties[ProblemDetailsMembers.CorrelationId] = new OpenApiSchema
            {
                Type = JsonSchemaType.String,
                Description = "Identifies the request in the server logs; quote it when reporting a problem.",
            };
            schema.Properties[ProblemDetailsMembers.TraceId] = new OpenApiSchema { Type = JsonSchemaType.String };
            schema.Properties["status"] = new OpenApiSchema { Type = JsonSchemaType.Integer, Format = "int32" };

            schema.Required ??= new HashSet<string>();
            schema.Required.Add(ProblemDetailsMembers.Code);
            schema.Required.Add(ProblemDetailsMembers.CorrelationId);
            schema.Required.Add("status");

            return Task.CompletedTask;
        });
    }
}
