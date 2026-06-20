using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Bluewater.Api.OpenApi;

public class DefaultResponsesTransformer : IOpenApiOperationTransformer
{
    public async Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        // Generate schema for error responses
        var validationProblemDetailsSchema = await context.GetOrCreateSchemaAsync(typeof(ValidationProblemDetails), null, cancellationToken);
        context.Document?.AddComponent("ValidationProblemDetails", validationProblemDetailsSchema);

        var problemDetailsSchema = await context.GetOrCreateSchemaAsync(typeof(ProblemDetails), null, cancellationToken);
        context.Document?.AddComponent("ProblemDetails", problemDetailsSchema);
        
        operation.Responses ??= new OpenApiResponses();
        // Add a "4XX" response to the operation with the newly created schema
        operation.Responses["400"] = new OpenApiResponse
        {
            Description = "Bad Request",
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["application/problem+json"] = new OpenApiMediaType
                {
                    Schema = new OpenApiSchemaReference("ValidationProblemDetails", context.Document)
                }
            }
        };
        
        operation.Responses["401"] = new OpenApiResponse
        {
            Description = "Unauthorized"
        };
        
        operation.Responses["403"] = new OpenApiResponse
        {
            Description = "Forbidden"
        };
        
        operation.Responses["500"] = new OpenApiResponse
        {
            Description = "Internal Error",
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["application/problem+json"] = new OpenApiMediaType
                {
                    Schema = new OpenApiSchemaReference("ProblemDetails", context.Document)
                }
            }
        };
    }
}