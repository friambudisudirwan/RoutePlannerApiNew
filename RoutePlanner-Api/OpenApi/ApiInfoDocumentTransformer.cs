using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace RoutePlanner_Api.OpenApi;

/// <summary>
/// Sets OpenAPI document title, version, and description.
/// </summary>
internal sealed class ApiInfoDocumentTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Info = new OpenApiInfo
        {
            Title = "Route Planner API",
            Version = "v1",
            Description =
                """
                API for route planning and runsheet integration with TMS EasyGO.

                ## Authentication
                1. Call `POST /api/Auth/Login` with `user_id` and `password`.
                2. Copy the returned `token`.
                3. Click **Authorize** in Scalar and enter: `Bearer {token}`
                   (or just `{token}` if the UI prepends Bearer automatically).

                ## Controllers
                - **Auth** — obtain JWT
                - **Planner** — generic create / integrate runsheets
                - **PrambananRoutePlan** — Prambanan-specific planning, PS update, and TMS integration
                """
        };

        return Task.CompletedTask;
    }
}
