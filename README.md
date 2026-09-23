# BridgeTask – Countries & Cities API

ASP.NET Core Web API for managing Countries and their Cities: CRUD, filtering, pagination, validation and consistent error responses, backed by SQL Server through Entity Framework Core.

## Technologies

- .NET 8, ASP.NET Core Web API (controllers)
- Entity Framework Core 8 with SQL Server (LocalDB by default)
- Mapster for request-to-entity mapping
- Swagger / OpenAPI (Swashbuckle)
- xUnit + `WebApplicationFactory` for tests

## Architecture

A practical N-layer structure. Dependencies point inwards: `Api → Application → Domain`, and `Infrastructure → Application`.

```
src/
  BridgeTask.Domain          Entities (Country, City) and their length limits
  BridgeTask.Application     DTOs, validators, services, exceptions, pagination, IAppDbContext
  BridgeTask.Infrastructure  AppDbContext, Fluent API configurations, migrations
  BridgeTask.Api             Controllers, global exception handler, Program.cs
tests/
  BridgeTask.Tests           API/integration tests against SQL Server + unit tests
postman/
  BridgeTask.postman_collection.json
```

The Application layer is organised by feature (`Countries/`, `Cities/`, `Common/`), so each entity's DTOs, validator, service and mapping sit together.

There is no generic repository or unit-of-work layer. `DbContext` already provides both, so services query through a small `IAppDbContext` interface (`Countries`, `Cities`, `SaveChangesAsync`). Services keep full use of `IQueryable`, and the Application layer never references SQL Server.

## Data Model

```
Country 1 ──── * City
```

| Table | Column | Rules |
|---|---|---|
| Countries | Id | Primary key (identity) |
| | Name | `nvarchar(100)`, required, **unique** (`IX_Countries_Name`) |
| | Code | `varchar(3)`, required, **unique** (`IX_Countries_Code`), stored upper-case |
| Cities | Id | Primary key (identity) |
| | Name | `nvarchar(100)`, required |
| | CountryId | Required FK → Countries.Id |
| | (CountryId, Name) | **Composite unique** (`IX_Cities_CountryId_Name`) |

The composite index means "Cairo" can exist only once in Egypt, while the same name is allowed in a different country. `CountryId` is the leading column, so the same index also serves foreign-key lookups and `countryId` filtering; no separate FK index is needed.

### Delete behaviour

The foreign key uses `DeleteBehavior.Restrict` (`ON DELETE NO ACTION` in SQL Server). Deleting a country never removes its cities as a side effect. `DELETE /api/countries/{id}` on a country that still has cities returns **409 Conflict**: *"Country cannot be deleted because it contains cities."*

## Validation Strategy

Validation happens at three levels. Each one catches something the others cannot.

1. **Data Annotations** on request DTOs check the shape of the request: required fields, lengths, letters-only code, positive `CountryId`, and paging ranges. `[ApiController]` rejects invalid input automatically with a 400 `ValidationProblemDetails`, so controllers contain no `ModelState` checks.
2. **Business validators** (`CountryValidator`, `CityValidator`), called by the services, check rules that need the database: unique code and name, unique city per country, referenced country exists, no cities before deleting a country. On update, the record itself is excluded, so saving a country with its own code is not a duplicate.
3. **Database constraints** are the final guarantee. Two concurrent requests can both pass step 2 before either inserts. The unique indexes and the FK make the second one fail. `AppDbContext.SaveChangesAsync` translates SQL Server errors 2601/2627 (unique) and 547 (FK) into a `ConflictException` with a safe message, so the client still gets a clean 409 and never a 500 or raw SQL text. A test sends 8 concurrent creates with the same code and asserts exactly one 201 and seven 409s.

Input is normalised before validation: names are trimmed and codes upper-cased.

## Exception Handling & ProblemDetails

Controllers have no try/catch. Errors flow like this:

```
Controller → Service → Validator / DbContext → exception → GlobalExceptionHandler (IExceptionHandler) → ProblemDetails
```

| Exception | Status | Example |
|---|---|---|
| `NotFoundException` | 404 | Country with id 5 was not found. |
| `ConflictException` | 409 | A country with code 'EG' already exists. |
| `BusinessValidationException` | 400 | Country with id 99 does not exist. (city request refers to a missing country) |
| Model validation (automatic) | 400 | `errors: { "Code": [...] }` |
| Anything else | 500 | An unexpected error occurred. |

Every error is `application/problem+json` with `type`, `title`, `status`, `detail`, `instance` and `traceId`:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.10",
  "title": "Resource conflict",
  "status": 409,
  "detail": "Country cannot be deleted because it contains cities.",
  "instance": "/api/countries/1",
  "traceId": "00-4e1f746e4cdabcb219e6372382aa578f-b77588fd55444aa9-00"
}
```

Unexpected exceptions are logged server-side with full details. The response only contains a generic message and the `traceId`, which ties it to the log entry. Stack traces, SQL, connection strings and exception types are never returned; a test checks this against an unreachable database.

A missing country referenced **in a request body** (creating or moving a city) is a 400, because the request is invalid. A missing country **in the URL** (`/api/countries/{id}/cities`) is a 404.

## Filtering & Search

All filters are optional and can be combined. Each one is added to the `IQueryable` only when supplied, so filtering, ordering and paging all run in SQL.

| Endpoint | Parameter | Behaviour |
|---|---|---|
| `GET /api/countries` | `name` | Exact match (case-insensitive) |
| | `code` | Exact match (case-insensitive) |
| | `search` | Partial match on **name or code** |
| `GET /api/cities` | `countryId` | Exact match |
| | `name` | Exact match on city name (case-insensitive) |
| | `search` | Partial match on **city name only**, so searching "egy" doesn't return every Egyptian city |

`search` is parameterised and escaped (`LIKE … ESCAPE`), so `%` or `_` in user input are treated as literal characters.

```
GET /api/countries?search=egy
GET /api/countries?name=Egypt&code=EG&pageNumber=1&pageSize=10
GET /api/cities?countryId=1&name=Cairo
GET /api/cities?search=cai&pageSize=20
```

Adding a future filter (for example `cityId` on a Company endpoint) means one nullable property on the filter DTO and one `if` in the service.

## Pagination

- `pageNumber` (default 1, minimum 1) and `pageSize` (default 10, range 1–100). Values out of range return 400; they are not silently clamped.
- Results are always ordered by `Name`, then `Id`, so pages are stable even when names repeat. `ToPagedResultAsync` only accepts an `IOrderedQueryable`, so unordered paging does not compile.
- One `COUNT` query plus one `OFFSET/FETCH` query.

```json
{
  "items": [],
  "pageNumber": 1,
  "pageSize": 10,
  "totalCount": 43,
  "totalPages": 5,
  "hasPreviousPage": false,
  "hasNextPage": true
}
```

## Mapping & EF Core Performance

- **Reads use explicit `Select` projection**, not a mapper. `city.Country.Name` in the projection makes EF generate a JOIN that selects only the five `CityDto` columns, with no `Include` and no entity graph. For city lists, EF pages the `Cities` rows first and joins `Countries` only for the rows on the page.
- **Writes use Mapster** (`dto.Adapt<Country>()`, `dto.Adapt(existing)`), and the mapping config is the single place where names are trimmed and codes upper-cased. Mapster was chosen over AutoMapper because it needs no configuration for matching names and is MIT-licensed.
- `AsNoTracking()` on all read-only queries. Tracking is used only when an entity is loaded to be updated or deleted.
- Everything is async end-to-end, and a `CancellationToken` is passed from the controller down to EF.
- No explicit transactions: each operation performs a single `SaveChangesAsync`, which EF already wraps in a transaction.
- No `RowVersion` concurrency token. For this reference data, last-write-wins on updates is acceptable, and the rules that matter (uniqueness and referential integrity) are enforced by database constraints regardless of timing. A `RowVersion` column would be the next step if clients needed to detect lost updates.

## Setup

**Prerequisites:** .NET 8 SDK and SQL Server LocalDB (installed with Visual Studio), or any SQL Server instance.

### Connection string

`src/BridgeTask.Api/appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=BridgeTaskDb;Trusted_Connection=True;TrustServerCertificate=True"
}
```

It uses Windows authentication, so no credentials are stored in the repository. To use another server without editing the file, set the `ConnectionStrings__DefaultConnection` environment variable or use `dotnet user-secrets`.

### Database migration

The API does not migrate on startup. Schema changes are applied deliberately:

```bash
dotnet tool install --global dotnet-ef
dotnet ef database update --project src/BridgeTask.Infrastructure --startup-project src/BridgeTask.Api
```

Migration: `InitialCreate` (in `src/BridgeTask.Infrastructure/Persistence/Migrations`).

### Run the API

```bash
dotnet run --project src/BridgeTask.Api --launch-profile http
```

The API listens on `http://localhost:5147`, and Swagger UI is at `http://localhost:5147/swagger`.

### Run the tests

```bash
dotnet test
```

The API tests run the real application against a real SQL Server database, so unique indexes, the restrict FK and SQL error translation are tested as they behave in production. Each run creates a uniquely named `BridgeTask_Tests_<guid>` database on LocalDB, applies the migration, and drops the database at the end.

To use another server, set `BRIDGETASK_TEST_SERVER` to a connection string without a database name. The tests always generate their own database name, so they cannot drop an existing database.

The suite has 66 tests:
- all Country and City scenarios
- a concurrent duplicate-create race
- database constraint enforcement
- ProblemDetails format
- a 500 response that must not leak internal details
- unit tests for the exception handler and pagination metadata

## Postman Collection

Import `postman/BridgeTask.postman_collection.json`. The `baseUrl` collection variable defaults to `http://localhost:5147`.

The folders are **Countries**, **Cities**, **Validation**, **Error Scenarios** and **Cleanup**. Run the whole collection in order with the Collection Runner or newman:

```bash
npx newman run postman/BridgeTask.postman_collection.json
```

How the collection works:
- Requests pass created ids to each other through collection variables.
- Each run uses a random country code, so it can be repeated against the same database.
- The Cleanup folder removes the data the run created.
- Every request checks its status code and response body.
- A collection-level test checks that every error response is valid ProblemDetails.

## API Endpoints

| Method | Route | Success | Errors |
|---|---|---|---|
| POST | `/api/countries` | 201 + Location | 400, 409 |
| GET | `/api/countries/{id}` | 200 | 404 |
| GET | `/api/countries?search=&name=&code=&pageNumber=&pageSize=` | 200 | 400 |
| PUT | `/api/countries/{id}` | 200 | 400, 404, 409 |
| DELETE | `/api/countries/{id}` | 204 | 404, 409 (has cities) |
| GET | `/api/countries/{countryId}/cities?pageNumber=&pageSize=` | 200 | 400, 404 |
| POST | `/api/cities` | 201 + Location | 400, 409 |
| GET | `/api/cities/{id}` | 200 | 404 |
| GET | `/api/cities?search=&name=&countryId=&pageNumber=&pageSize=` | 200 | 400 |
| PUT | `/api/cities/{id}` | 200 | 400, 404, 409 |
| DELETE | `/api/cities/{id}` | 204 | 404 |

Updates return **200 with the updated resource**, so clients see the normalised values.
