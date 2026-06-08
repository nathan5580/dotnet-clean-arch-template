using Microsoft.OpenApi.Models;

namespace Api.OpenApi;

public sealed class OpenApiDocumentTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken ct)
    {
        document.Info = new OpenApiInfo
        {
            Title = "{{ProjectName}} API",
            Version = "v1",
            Description = "API for {{ProjectName}}"
        };

        return Task.CompletedTask;
    }
}
