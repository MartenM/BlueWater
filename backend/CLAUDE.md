# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

This is a .NET 10 solution (`Bluewater.sln`) built with the standard SDK CLI — there is no separate build script.

```
dotnet build Bluewater.sln                       # build everything
dotnet run --project src/Bluewater.Api           # run the API (http://localhost:5066, https://localhost:7292)
dotnet test src/Bluewater.Tests/Bluewater.Tests.csproj   # run the test suite
```

There is no lint/format config beyond the default SDK analyzers.

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

### Testing conventions

`src/Bluewater.Tests` (xUnit + Shouldly assertions) holds the tests. **Whenever you add or change a service** (an interface + implementation in `Bluewater.Core` or `Bluewater.Infra`), add or update its test class under `Tests/Services/` in the same change — don't treat this as a follow-up task.

- **DB-backed services** (anything depending on `BluewaterContext` and/or `UserManager<BlueUser>`, which is most of them) get a test class extending `TestSupport/SqliteServiceTestBase`. That base spins up a fresh in-memory SQLite database per test via `Database.EnsureCreated()` against the live EF model (not the Npgsql-flavored migrations — SQLite can't run those), and wires up the same `AddIdentityCore<BlueUser>()` stack `AddBluewater()` uses.
  - Register the service's interface in the base class's `ServiceCollection` (mirroring `AddBluewater()`), then resolve the SUT in the test via `GetService<IYourService>()` — never construct it with `new`. This way a test keeps working if the constructor later gains a dependency that's already registered globally (e.g. an `ILogger<T>`), instead of breaking on every unrelated signature change.
  - Reuse existing base-class helpers (`CreateUserAsync`, `CreateCurrentSeasonAsync`) for shared fixtures (a user, a current season + `BlueAppSettings` row); only add a new shared helper if more than one test class will need it, otherwise build the fixture inline/private to that test class.
  - Cover the happy path, the `BlueNotFoundException` case, and any `BlueValidationException` validation branches for CRUD-style services. For services with side effects beyond the DTO they return (e.g. `AuthService` hashing/rotating refresh tokens), assert those side effects directly against `Db`.
- **Pure-logic services** with no DB/HTTP dependency (e.g. `TokenService`, `CurrentUserService`) skip the SQLite base entirely — construct them directly (`Options.Create(...)`, a 3-line hand-rolled fake for a single-property interface like `IHttpContextAccessor`). Don't reach for a mocking library over this.
- Run with `dotnet test src/Bluewater.Tests/Bluewater.Tests.csproj`.

### State of the scaffold

Several pieces referenced by the architecture are not yet wired into DI/`Program.cs`: there is no `AddDbContext`/Npgsql registration, no `AddIdentity`, no `AddAuthentication`/JWT bearer setup, and no `Jwt` section in `appsettings.json` (which `TokenOptions.ValidateOnStart()` requires). `AuthController` and `Filters/` are currently empty placeholders. When building out auth, expect to add these registrations in `AddBluewater()` and the corresponding config in `appsettings.json`/`appsettings.Development.json`.
