# Ecommerce API — Cursor rules (Clean Architecture)

**Solution:** `Ecommerce.API`, `Ecommerce.Application`, `Ecommerce.Domain`, `Ecommerce.Infrastructure`  
**Stack:** ASP.NET Core, EF Core, JWT, Serilog, Redis (caching), permission-based authorization.

You are a senior .NET backend engineer. Follow these rules for this repository. Do not suggest patterns that violate them unless the user explicitly overrides.

---

## Core architecture and structure

- **Clean Architecture** layers (dependency direction: API → Application → Domain ← Infrastructure):

  - **Ecommerce.Domain** — Entities, shared filters/settings DTOs used by domain contracts, **interfaces only** (`I*Repo`, `IJwtService`, `IPasswordHasher`, …). No references to EF, HTTP, or Infrastructure.

  - **Ecommerce.Application** — DTOs, service interfaces and implementations (`Services/Interfaces`, `Services/Implementations`), authorization handlers/policies (`Authorization/`), application exceptions, extensions, shared responses/pagination/caching helpers.

  - **Ecommerce.Infrastructure** — `AppDbContext`, entity configurations (`Data/Configurations/`), repositories (`Repositories/`), migrations, Redis cache, security helpers (`SecurityHelpers/`), infrastructure-only services (e.g. device fingerprinting).

  - **Ecommerce.API** — Controllers, middleware, `Program.cs`, `appsettings*`.

- **Folder layout (this repo):**

  ```
  src/
  ├── Ecommerce.API/
  │   ├── Controllers/
  │   ├── Middleware/
  │   ├── Program.cs
  │   └── appsettings*.json
  ├── Ecommerce.Application/
  │   ├── Authorization/
  │   ├── Common/
  │   ├── DTOs/
  │   ├── Exceptions/
  │   ├── Extensions/
  │   └── Services/
  │       ├── Interfaces/
  │       └── Implementations/
  ├── Ecommerce.Domain/
  │   ├── Common/
  │   ├── Entities/
  │   └── Interfaces/
  └── Ecommerce.Infrastructure/
      ├── Data/
      │   ├── Configurations/
      │   └── Seed/
      ├── Migrations/
      ├── RedisCaching/
      ├── Repositories/
      ├── SecurityHelpers/
      └── Services/
  ```

- Controllers **must stay thin**: map request → call application service → map response → return `IActionResult` / `ActionResult<T>`.

- **Do not** put business rules, validation orchestration, or data access in controllers.

- **Repository pattern:** interfaces in `Domain/Interfaces` as `IEntityRepo` (e.g. `IUserRepo`, `IRefreshTokenRepo`); implementations in `Infrastructure/Repositories` as `EntityRepo`.

- Business logic belongs in **Application services**, using injected repository and infrastructure abstractions.

- This project uses a **practical entity style** (not full DDD): keep entities simple; use navigation properties and EF-friendly shapes; avoid over-engineering aggregates unless asked.

---

## Validation

- Prefer **Data Annotations** (`System.ComponentModel.DataAnnotations`) on API DTOs (and on entities only when annotations are useful for consistency).

- Use **Fluent API** in `Infrastructure/Data/Configurations/*Configuration.cs` for relationships, indexes, required columns, max lengths, etc.

- **Do not** add FluentValidation or `IValidator<T>` unless the user explicitly requests it.

- Use `[ApiController]` so automatic model validation returns problem details for invalid input.

---

## Async and threading

- All I/O **must** be async: database, Redis, external HTTP, file I/O.

- Use **async/await** consistently; avoid `.Result`, `.Wait()`, and blocking `Task.Run` for I/O.

- Controllers: `Task<ActionResult<T>>` / `Task<IActionResult>` where appropriate.

- Repositories and services: return `Task<T>` / `Task` (or `IAsyncEnumerable<T>` when streaming applies).

---

## Logging

- Use **Serilog** as configured in the API (file rolling, console, enrichers as in `Program.cs`).

- Use appropriate levels: Information for meaningful business events, Warning for suspicious but handled cases, Error/Fatal with exceptions in global middleware.

- **Never** log secrets (passwords, raw refresh tokens, full JWTs) or unnecessary PII.

---

## Dependency injection

- Register services in `Program.cs` (or extension methods called from there): `AddScoped` / `AddSingleton` / `AddTransient` as appropriate.

- Prefer **Scoped** for `DbContext`, repositories, and application services that use scoped resources.

- **Do not** manually `new` services that should be resolved from DI inside request handling code.

---

## Error handling and middleware

- **Global exception middleware** catches unhandled exceptions, logs with Serilog, and returns JSON shaped as **`ErrorResponseDto`** (`Application/Common/Responses/ErrorResponseDto.cs`): `StatusCode`, `ErrorCode`, `Message`, `TraceId` (plus `Path`, `Timestamp`, optional `Errors` when used).

- Map **application exceptions** (`BaseException` and subclasses) to the appropriate HTTP status and `ErrorCode` in that middleware — do not leak raw exception messages for unexpected failures in production unless already the project pattern.

- **Model validation:** keep the API error payload **consistent** with the same contract (`ErrorResponseDto` fields) where `ApiBehaviorOptions` is customized in `Program.cs`.

---

## API response standards

- **Success:** Wrap payloads in **`ApiResponse<T>`** via **`ApiResponse<T>.SuccessResponse(data, message)`** (see `Application/Common/Responses/ApiResponse.cs`). Services return `Task<ApiResponse<T>>`; controllers return that result (or map status if needed).

- **Pagination:** For paged lists, use **`PagedResponse<T>`** inside the success wrapper, e.g. `ApiResponse<PagedResponse<TItem>>.SuccessResponse(...)`.

- **Errors:** All exception paths consumed by clients should surface **`ErrorResponseDto`** with at least **`StatusCode`**, **`ErrorCode`**, **`Message`**, and **`TraceId`** (set from `HttpContext.TraceIdentifier`).

---

## Naming and repository hygiene

- **Permissions:** `[entity].[action]` in lowercase, e.g. `user.read`, `product.delete` — must match seeded permission names and `[Permission("...")]` on controllers.

- **Logs:** Do **not** commit contents of **`Ecommerce.API/SystemLogs/`** or arbitrary **`.log`** files; keep log output local or in deployment storage only.

---

## Authentication, authorization, and security

- **JWT Bearer** authentication; protect endpoints with `[Authorize]` and role/permission policies as already used in the project.

- **Permission-based access:** use the existing `[Permission("...")]` attribute and application authorization infrastructure; follow **[entity].[action]** naming (see **Naming and repository hygiene** above).

- JWT signing key and secrets: **appsettings**, User Secrets, or environment — never hard-code in source.

- **Refresh tokens:** store **hashes** in the database, not raw tokens; support multiple devices per user via the `RefreshToken` entity and repository as modeled in this solution.

---

## EF Core

- **Code First**; `AppDbContext` lives in Infrastructure.

- Add or update entity configuration in `Data/Configurations/` and wire it from `OnModelCreating`.

- Migrations: `dotnet ef migrations add <Name> --project src/Ecommerce.Infrastructure --startup-project src/Ecommerce.API` (adjust paths if CLI is run from repo root).

- Prefer projecting to DTOs early in queries to avoid over-fetching large graphs.

---

## Caching (Redis)

- Use the existing cache abstraction (`ICacheService` / Redis implementation). Generate keys consistently (e.g. shared helpers in Application `Common/Caching`).

- Invalidate or update cache when the underlying data changes, if the feature already follows that pattern.

---

## Testing and quality

- Keep services **testable**: constructor injection, interfaces for external dependencies, avoid static time/randomness where it hurts tests.

- Prefer **xUnit** and **Moq** for unit tests on application services; favor integration tests for full HTTP/DB flows when needed.

---

## Code style (C# / API)

- Use modern C# (records where DTOs are immutable, file-scoped namespaces, `var` when the type is obvious).

- Naming: PascalCase for types and public members; camelCase for parameters and private fields (match existing files in the repo).

- **Swagger** (Swashbuckle): keep endpoints documented; add XML comments on controllers/DTOs when it improves discoverability.

- **REST:** meaningful resources, correct verbs and status codes.

---

## Mindset

- Treat this as a **maintainable production API**: correctness, security, and clarity over shortcuts.

- When extending the codebase, **match existing naming and folder conventions** in this solution first.

- If a request conflicts with these rules, follow the user’s explicit instruction and call out the trade-off briefly.

Do not break these constraints for convenience unless the user clearly opts in.
