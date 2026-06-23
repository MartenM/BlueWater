using System.Text.Json.Serialization;
using Bluewater.Api.Extensions;
using Bluewater.Api.Filters;
using Bluewater.Api.OpenApi;
using RealmRuler.WebApi.OpenApi;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi(options =>
{
    options.AddOperationTransformer<DefaultResponsesTransformer>();
    options.AddOperationTransformer<FormFileOperationTransformer>();
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
    options.AddSchemaTransformer<EnumSchemaTransformer>();
});

builder.Services.AddControllers(options =>
{
    options.Filters.Add<UnauthorizedAccessExceptionFilter>();
    options.Filters.Add<BlueValidationExceptionFilter>();
    options.Filters.Add<FluentValidationExceptionFilter>();
    options.Filters.Add<BlueNotFoundExceptionFilter>();
    options.Filters.Add<FileNotFoundExceptionFilter>();
})
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(
        new JsonStringEnumConverter());
});;

builder.AddBluewater();


var app = builder.Build();

await app.UseBluewater();

// Configure the HTTP request pipeline.
app.MapOpenApi();
app.MapScalarApiReference();

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Root redirects to /scalar
app.MapGet("/", () => Results.Redirect("/scalar"))
    .ExcludeFromDescription(); // ensures no OpenAPI entry

app.Run();