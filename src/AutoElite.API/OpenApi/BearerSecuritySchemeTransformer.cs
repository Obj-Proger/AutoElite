using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace AutoElite.API.OpenApi;

/// <summary>
/// Describes the access token in the OpenAPI document: a bearer JWT in the Authorization header, which the API reader
/// lets one enter. The token is optional for the document as a whole, since the application, not the route, decides
/// which requests need a signed-in user.
/// </summary>
internal sealed class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    private const string SchemeName = "Bearer";

    /// <inheritdoc />
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes[SchemeName] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "The access token of a session, from POST /api/v1/auth/login or /api/v1/auth/refresh."
        };

        // Either the token or nothing: an empty requirement lets a request go without it.
        document.Security =
        [
            new OpenApiSecurityRequirement { [new OpenApiSecuritySchemeReference(SchemeName, document)] = [] },
            new OpenApiSecurityRequirement()
        ];

        return Task.CompletedTask;
    }
}
