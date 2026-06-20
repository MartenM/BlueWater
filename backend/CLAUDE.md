# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

This is a .NET 10 solution (`Bluewater.sln`) built with the standard SDK CLI — there is no separate build script.

```
dotnet build Bluewater.sln                       # build everything
dotnet run --project src/Bluewater.Api           # run the API (http://localhost:5066, https://localhost:7292)
```

There is currently no test project in the solution and no lint/format config beyond the default SDK analyzers.

EF Core migrations live in `src/Bluewater.Infra/Migrations` (empty so far). Once a connection string and `AddDbContext` registration exist, migrations would be managed from the `Bluewater.Infra` project, e.g.:

```
dotnet ef migrations add <Name> --project src/Bluewater.Infra --startup-project src/Bluewater.Api
dotnet ef database update --project src/Bluewater.Infra --startup-project src/Bluewater.Api
```

## Architecture

Layered solution, one csproj per layer, dependencies flow in one direction:

```
Bluewater.Api  -->  Bluewater.Core  -->  Bluewater.Infra  -->  Bluewater.Domain
```

- **Bluewater.Domain** — POCOs only. ASP.NET Core Identity entities (`BlueUser : IdentityUser<Guid>`, `BlueRole : IdentityRole<Guid>` in `Models/Identity`) and plain domain models (`RefreshToken` in `Models/Auth`). No EF or service references — keep it dependency-light.
- **Bluewater.Infra** — EF Core/Identity infrastructure: `Context/BluewaterContext` (an `IdentityDbContext<BlueUser, BlueRole, Guid>` with a `RefreshTokens` DbSet, configured for Npgsql/Postgres), `Services/TokenService` (JWT issuance via `JsonWebTokenHandler`), and `Options/TokenOptions` (bound from config section `"Jwt"`).
- **Bluewater.Core** — application services and DTOs consumed by the API. `Services/AuthService` implements `IAuthService` (login/refresh/logout against `UserManager<BlueUser>` + `BluewaterContext`, with refresh tokens stored as SHA-256 hashes and rotated on each refresh). `Services/CurrentUserService` implements `ICurrentUserService` (per-request identity, backed by `IHttpContextAccessor`). DTOs live under `Dto/Auth` (`LoginRequest`, `AuthResponse`).
- **Bluewater.Api** — ASP.NET Core Web API host. `Extensions/WebApplicationBuilderExtensions.AddBluewater()` is the single composition-root method that wires up options binding and DI (`TokenOptions`, `ICurrentUserService`, `IAuthService`) — add new service registrations here rather than directly in `Program.cs`. Controllers live under `Controllers/` (top-level, e.g. `HealthController`) and `Controllers/Api/` (versioned/API-specific, e.g. `AuthController`). OpenAPI is generated via `Microsoft.AspNetCore.OpenApi` with a custom `IOpenApiOperationTransformer` (`OpenApi/DefaultResponsesTransformer`) that adds shared `4xx`/`5xx` `ProblemDetails` responses to every operation, and is served through Scalar at `/scalar` (root `/` redirects there).

### Controller conventions

- Controllers stay thin: no `try`/`catch` and no business logic — they call straight into a `Core` service and return its result.
- Exceptions are translated to HTTP responses by global exception filters (`Filters/`, registered in `Program.cs` via `AddControllers(options => options.Filters.Add<...>())`), not by per-action error handling. Each exception type gets its own filter (e.g. `BlueValidationException` → `BlueValidationExceptionFilter`, `UnauthorizedAccessException` → `UnauthorizedAccessExceptionFilter`).
- Actions return the plain success type, not `IActionResult`/`ActionResult<T>`: `Task<SomeResponse>` for actions with a body, or bare `Task` for actions with none. Returning `Task` (no body) yields an implicit `200 OK` — that's the default for actions without a meaningful response payload, so most actions just return OK. This keeps OpenAPI response schemas accurate without manual annotation.

### State of the scaffold

Several pieces referenced by the architecture are not yet wired into DI/`Program.cs`: there is no `AddDbContext`/Npgsql registration, no `AddIdentity`, no `AddAuthentication`/JWT bearer setup, and no `Jwt` section in `appsettings.json` (which `TokenOptions.ValidateOnStart()` requires). `AuthController` and `Filters/` are currently empty placeholders. When building out auth, expect to add these registrations in `AddBluewater()` and the corresponding config in `appsettings.json`/`appsettings.Development.json`.
