using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Bluewater.Api.OpenApi;

public class EnumSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(
        OpenApiSchema schema,
        OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken)
    {
        var type = context.JsonTypeInfo?.Type;

        if (type is null || !type.IsEnum)
            return Task.CompletedTask;

        schema.Type = JsonSchemaType.String;

        schema.Enum = Enum.GetNames(type)
            .Select(name => (JsonNode) JsonValue.Create(name)!)
            .ToList();

        return Task.CompletedTask;
    }
}