using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Bluewater.Api.OpenApi;

/// <summary>
/// ASP.NET Core's ApiExplorer flattens [FromForm] IFormFile parameters into their individual CLR
/// properties (ContentType, Length, FileName, etc.) before the OpenAPI generator sees them, so by the
/// time operation transformers run there's no parameter left that's actually typed IFormFile - the
/// request body ends up described as application/x-www-form-urlencoded with those reflected fields.
/// This reads the controller action's real parameters via reflection instead, and rewrites the request
/// body to a proper multipart/form-data schema so generated clients (nswag) emit real file-upload code.
/// </summary>
public class FormFileOperationTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        if (context.Description.ActionDescriptor is not ControllerActionDescriptor controllerAction)
            return Task.CompletedTask;

        var formParameters = controllerAction.MethodInfo.GetParameters()
            .Where(p => p.GetCustomAttributes(typeof(FromFormAttribute), false).Length > 0)
            .ToList();

        if (formParameters.Count == 0 || formParameters.All(p => p.ParameterType != typeof(IFormFile)))
            return Task.CompletedTask;

        var properties = new Dictionary<string, IOpenApiSchema>();
        var required = new HashSet<string>();

        foreach (var parameter in formParameters)
        {
            var isFile = parameter.ParameterType == typeof(IFormFile);
            properties[parameter.Name!] = new OpenApiSchema
            {
                Type = JsonSchemaType.String,
                Format = isFile ? "binary" : null
            };
            required.Add(parameter.Name!);
        }

        operation.RequestBody = new OpenApiRequestBody
        {
            Required = true,
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["multipart/form-data"] = new OpenApiMediaType
                {
                    Schema = new OpenApiSchema
                    {
                        Type = JsonSchemaType.Object,
                        Properties = properties,
                        Required = required
                    }
                }
            }
        };

        return Task.CompletedTask;
    }
}
