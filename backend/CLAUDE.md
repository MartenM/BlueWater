# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

This is a .NET 10 solution (`Bluewater.sln`) built with the standard SDK CLI — there is no separate build script.

```
dotnet build Bluewater.sln                       # build everything
dotnet run --project src/Bluewater.Api           # run the API (http://localhost:5066, https://localhost:7292)
dotnet test src/Bluewater.Tests/Bluewater.Tests.csproj   # run the test suite
dotnet test src/Bluewater.Tests/Bluewater.Tests.csproj --filter "FullyQualifiedName~ServiceName"  # targeted (preferred per-change)
```

## Key file locations

| What | Where |
|---|---|
| New service interface | `src/Bluewater.Core/Services/Abstractions/I<Name>Service.cs` |
| New service implementation | `src/Bluewater.Core/Services/<Name>Service.cs` |
| Register service in DI | `src/Bluewater.Api/Extensions/WebApplicationBuilderExtensions.cs` |
| New controller | `src/Bluewater.Api/Controllers/Api/<Name>Controller.cs` |
| New DTO | `src/Bluewater.Core/Dto/<Feature>/` |
| New validator | `src/Bluewater.Core/Validators/<Name>Validator.cs` (auto-scanned, no extra registration) |
| New domain model | `src/Bluewater.Domain/Models/<Feature>/` |
| New migration | `dotnet ef migrations add <Name> --project src/Bluewater.Infra --startup-project src/Bluewater.Api` |
| New test class | `src/Bluewater.Tests/Services/<Name>ServiceTests.cs` |

## Don'ts

- Don't add `try`/`catch` in controllers — exception filters in `Filters/` handle all error-to-HTTP mapping
- Don't curl-test thin controllers — service tests (`SqliteServiceTestBase`) are sufficient
- Don't run the full test suite per change — target the affected service's test class with `--filter`
- Don't hand-register validators — `AddValidatorsFromAssemblyContaining<>` in `WebApplicationBuilderExtensions` auto-scans all validators in `Bluewater.Core`

There is no lint/format config beyond the default SDK analyzers.

The API requires a running Postgres instance matching the `Database` section of `appsettings.Development.json` (host/port/database/username/password) — there's no Docker Compose in this repo yet, so point it at a local or external Postgres. On startup the API runs `context.Database.Migrate()` automatically (see `WebApplicationExtensions.UseBluewater()`), and in the `Development` environment seeds an initial admin user/season/groups if the `Users` table is empty (`BluewaterContextSeeder`) — seeded admin login is `admin` / `admin`.

EF Core migrations live in `src/Bluewater.Infra/Migrations` and are managed from the `Bluewater.Infra` project:

```
dotnet ef migrations add <Name> --project src/Bluewater.Infra --startup-project src/Bluewater.Api
dotnet ef database update --project src/Bluewater.Infra --startup-project src/Bluewater.Api
```

## Architecture

Layered solution, one csproj per layer, dependencies flow in one direction:

```
Bluewater.Api  -->  Bluewater.Core  -->  Bluewater.Infra  -->  Bluewater.Domain
```

- **Bluewater.Domain** — POCOs only, no EF/service references. ASP.NET Core Identity entities (`BlueUser : IdentityUser<Guid>`, `BlueRole : IdentityRole<Guid>` in `Models/Identity`), plain domain models (`RefreshToken`, `BlueSeason`, `BlueAppSettings`, the `Models/Groups` group/permission entities, `Models/Files/StoredFile`), and cross-cutting contracts: `Auditing/IAuditable` + `IAuditableRelation` (soft-delete/audit-stamp markers consumed by `BluewaterContext`) and `Auth/BlueClaimTypes` (the `"permission"` claim type used by the permission system).
- **Bluewater.Infra** — EF Core/Identity infrastructure:
  - `Context/BluewaterContext` — an `IdentityDbContext<BlueUser, BlueRole, Guid>` configured for Npgsql/Postgres. Overrides `SaveChanges`/`SaveChangesAsync` to auto-stamp `IAuditable` entities (`CreatedAt/By`, `UpdatedAt/By`, soft-delete via `DeletedAt/By` instead of a hard delete) using `ICurrentUserAccessor`, applies a global `HasQueryFilter(x => x.DeletedAt == null)` to every `IAuditable` entity, and revives soft-deleted composite-key rows (`IAuditableRelation`) instead of inserting a duplicate when the same key is re-added.
  - `Context/BluewaterContextSeeder` — Development-only seeding of an admin user, a current season, and default group/permission data.
  - `Services/TokenService` (JWT issuance via `JsonWebTokenHandler`), `Services/LocalFileStorageService` (`IFileStorageService` — stores uploaded files on local disk under `LocalFileStorageOptions.RootPath`).
  - `Options/`: `TokenOptions` (config section `"Jwt"`), `DatabaseOptions` (`"Database"` — built into an Npgsql connection string), `LocalFileStorageOptions` (`"FileStorage:Local"`).
- **Bluewater.Core** — application services and DTOs consumed by the API, under `Services/` (+ `Services/Abstractions` for interfaces), `Dto/<Feature>`, and `Validators/` (FluentValidation — see below). `Exceptions/BlueNotFoundException` and `BlueValidationException` are the two exception types services throw for client-facing errors (FluentValidation throws its own `ValidationException` instead — see below). See `Services/` for the current list of services.
- **Bluewater.Api** — ASP.NET Core Web API host.
  - `Extensions/WebApplicationBuilderExtensions.AddBluewater()` is the single composition-root method that wires up options binding and DI (add new service registrations here rather than directly in `Program.cs`) — Npgsql `DbContext`, CORS, `AddIdentityCore<BlueUser>()` + roles, JWT bearer authentication, the custom permission-based authorization policy provider/handler, and all Core/Infra service registrations.
  - `Extensions/WebApplicationExtensions.UseBluewater()` runs EF migrations on startup and triggers Development seeding; called from `Program.cs` between `builder.Build()` and the middleware pipeline.
  - `Authorization/` — a permission-based authz system layered on top of ASP.NET policies: `BlueAuthorizeAttribute(params BluePermission[])` builds a policy name like `"Permission:AdminViewGroups,AdminModifyGroups"`; `PermissionPolicyProvider` (`IAuthorizationPolicyProvider`) parses that name into a `PermissionRequirement`, falling back to the default provider for ordinary `[Authorize]` policies; `PermissionAuthorizationHandler` succeeds if the user has a `BlueClaimTypes.Permission` claim matching any required permission. `BluePermission` (`Bluewater.Domain.Models.Groups`) is the enum of permissions; use plain `[Authorize]` for "just needs to be logged in" endpoints and `[BlueAuthorize(BluePermission.X)]` for permission-gated ones.
  - Controllers live under `Controllers/` (top-level, e.g. `HealthController`) and `Controllers/Api/` (`AuthController`, `UserGroupCategoriesController`, `UserGroupsController`, `UserGroupInstancesController`, `UserProfilesController`, `FilesController`).
  - `Filters/` — one `IExceptionFilter` per exception type, registered in `Program.cs` via `AddControllers(options => options.Filters.Add<...>())`: `BlueValidationExceptionFilter`, `BlueNotFoundExceptionFilter`, `FileNotFoundExceptionFilter`, `UnauthorizedAccessExceptionFilter`. Each maps its exception to a `ProblemDetails`/`NotFoundObjectResult`-style response — controllers never `try`/`catch` themselves.
  - OpenAPI is generated via `Microsoft.AspNetCore.OpenApi` with custom transformers under `OpenApi/`: `DefaultResponsesTransformer` (adds shared `4xx`/`5xx` responses to every operation), `BearerSecuritySchemeTransformer` (documents the JWT bearer scheme), `EnumSchemaTransformer` (renders enums as string schemas). Served through Scalar at `/scalar` (root `/` redirects there).

### Domain model: groups, seasons, permissions

Permissions are not assigned directly to users. A `UserGroup` (e.g. "Members"), scoped under a `UserGroupCategory`, is instantiated per `BlueSeason` as a `UserGroupInstance` (unique per `UserGroupId` + `SeasonId`). Users are attached via `UserGroupInstanceMember`, and permissions are attached to the instance via `UserGroupInstancePermission` — so group membership and the permissions a group grants are both season-scoped. `BlueAppSettings` holds the single `CurrentSeasonId` row that most season-relative logic reads. Files (profile pictures, etc.) are tracked as `StoredFile` rows with content stored separately via `IFileStorageService`.

### Request validation with FluentValidation

For request DTOs with non-trivial validation (length limits, cross-field rules, or rules that need a DB lookup), write a `FluentValidation` `AbstractValidator<TRequest>` in `Bluewater.Core/Validators/` rather than hand-rolled checks in the service method. `UpsertNewsPostRequestValidator` (validating `UpsertNewsPostRequest`) is the reference example:

- One validator class per request DTO, named `<Request>Validator`, constructor-injecting whatever it needs (e.g. `BluewaterContext` for an existence check via `MustAsync`/`AnyAsync`) — validators are registered in DI like any other service, so they can take dependencies instead of being static rule sets.
- The owning service takes `IValidator<TRequest>` as a constructor dependency (see `NewsService`) and calls `await _validator.ValidateAndThrowAsync(request)` as the first line of any method that accepts that request type — do this in **every** method that takes the request (e.g. both `CreateAsync` and `UpdateAsync`), not just one.
- `ValidateAndThrowAsync` throws FluentValidation's own `ValidationException` (distinct from `Bluewater.Core.Exceptions.BlueValidationException`). It's translated to a `400` `ValidationProblemDetails` by `Bluewater.Api/Filters/FluentValidationExceptionFilter`, registered in `Program.cs` alongside the other exception filters — controllers and services never catch it themselves.
- Wire-up is one line per composition root: `services.AddValidatorsFromAssemblyContaining<UpsertNewsPostRequestValidator>()` scans `Bluewater.Core` for all validators and registers them as `IValidator<T>`. This call already exists in both `WebApplicationBuilderExtensions.AddBluewater()` and `SqliteServiceTestBase` — adding a new validator class to `Validators/` is picked up automatically by both, no extra registration needed.
- In tests, resolve the validator via DI like any other dependency when constructing the service (`GetService<IYourService>()`), and add a test case per validation rule alongside the happy-path/`BlueNotFoundException` cases described in Testing conventions below.

### Controller conventions

- Controllers stay thin: no `try`/`catch` and no business logic — they call straight into a `Core` service and return its result.
- Exceptions are translated to HTTP responses by the global exception filters described above, not by per-action error handling.
- Actions return the plain success type, not `IActionResult`/`ActionResult<T>`: `Task<SomeResponse>` for actions with a body, or bare `Task` for actions with none. Returning `Task` (no body) yields an implicit `200 OK` — that's the default for actions without a meaningful response payload, so most actions just return OK. `FilesController.Download` is the one exception (returns `IActionResult`/`File(...)` since it streams a binary body). This keeps OpenAPI response schemas accurate without manual annotation.
- Use `[Authorize]` for endpoints that just need an authenticated caller (e.g. "get my own data") and `[BlueAuthorize(BluePermission.X)]` for endpoints gated on a specific permission.

### Testing conventions

`src/Bluewater.Tests` (xUnit + Shouldly assertions) holds the tests. **Whenever you add or change a service** (an interface + implementation in `Bluewater.Core` or `Bluewater.Infra`), add or update its test class under `Tests/Services/` in the same change — don't treat this as a follow-up task.

- **DB-backed services** (anything depending on `BluewaterContext` and/or `UserManager<BlueUser>`, which is most of them) get a test class extending `TestSupport/SqliteServiceTestBase`. That base spins up a fresh in-memory SQLite database per test via `Database.EnsureCreated()` against the live EF model (not the Npgsql-flavored migrations — SQLite can't run those), and wires up the same `AddIdentityCore<BlueUser>()` stack `AddBluewater()` uses.
  - Register the service's interface in the base class's `ServiceCollection` (mirroring `AddBluewater()`), then resolve the SUT in the test via `GetService<IYourService>()` — never construct it with `new`. This way a test keeps working if the constructor later gains a dependency that's already registered globally (e.g. an `ILogger<T>`), instead of breaking on every unrelated signature change.
  - Reuse existing base-class helpers (`CreateUserAsync`, `CreateCurrentSeasonAsync`) for shared fixtures (a user, a current season + `BlueAppSettings` row); only add a new shared helper if more than one test class will need it, otherwise build the fixture inline/private to that test class. `CurrentUserId` sets the acting user for audit-stamping assertions; `FileStorageRootPath` points at a per-test temp directory cleaned up on `Dispose`.
  - Cover the happy path, the `BlueNotFoundException` case, and any `BlueValidationException` validation branches for CRUD-style services. For services with side effects beyond the DTO they return (e.g. `AuthService` hashing/rotating refresh tokens, soft-delete/revival on `IAuditableRelation` entities), assert those side effects directly against `Db`.
- **Pure-logic services** with no DB/HTTP dependency (e.g. `TokenService`, `CurrentUserService`) skip the SQLite base entirely — construct them directly (`Options.Create(...)`, a 3-line hand-rolled fake for a single-property interface like `IHttpContextAccessor`). Don't reach for a mocking library over this.
- `BluewaterContextAuditingTests` and `BluewaterContextRelationRevivalTests` cover the `BluewaterContext` auditing/soft-delete/revival behavior directly and are a good reference if you're touching that logic.
- Run with `dotnet test src/Bluewater.Tests/Bluewater.Tests.csproj`.
