# VerusLLC

A small company registry: an ASP.NET Core Web API (.NET 10, clean architecture + MediatR/CQRS) and an
Angular client. A company is accepted only when its name plausibly matches the host of its website.
Storage is in memory, so the data lives as long as the API process does.

## Requirements

| Tool | Version used | Notes |
| --- | --- | --- |
| .NET SDK | **10.0.401** | Projects target `net10.0` |
| Node.js | **24.12.0** | Angular 21 supports `^20.19.0 \|\| ^22.12.0 \|\| ^24.0.0` |
| npm | **11.6.2** | Bundled with Node 24 |
| Angular CLI | **21.0.3** | `npx ng` works without a global install |

Check yours with `dotnet --version`, `node --version`, `npm --version`.

The Node range comes from the official Angular compatibility table:
<https://angular.dev/reference/versions>.

> Prefer containers? See [Running with Docker Compose](#running-with-docker-compose) — it needs neither
> the .NET SDK nor Node installed.

## Layout

```
VerusLLC.slnx                 solution (new .slnx format)
Directory.Packages.props      central NuGet versions — csproj files carry no Version attribute
global.json                   opts dotnet test into the Microsoft.Testing.Platform runner
src/
  VerusLLC.Domain/            entities and business rules; no dependencies at all
  VerusLLC.Application/       MediatR handlers, Result/Error, repository contracts
  VerusLLC.Persistence/       in-memory repository implementation
  VerusLLC.Api/               controllers, filters, middleware, composition root
  VerusLLC.App/               Angular client
tests/
  VerusLLC.Tests/             unit + HTTP integration tests
```

Dependencies point inward only:

```
Api ──> Application ──> Domain
 └────> Persistence ──> Application
Tests ──> Api (and everything below it)
```

Where things live:

| Concern | Location |
| --- | --- |
| Entity and invariants | `src/VerusLLC.Domain/Companies/Company.cs` |
| Name–host relevance rule | `src/VerusLLC.Domain/Companies/CompanyNameHostPolicy.cs` |
| Domain error codes | `src/VerusLLC.Domain/Companies/CompanyErrors.cs` |
| Repository contract | `src/VerusLLC.Application/Common/Persistence/ICompanyRepository.cs` |
| Repository implementation | `src/VerusLLC.Persistence/Companies/InMemoryCompanyRepository.cs` |
| Command / query handlers | `src/VerusLLC.Application/Companies/{Commands,Queries}/` |
| Internal DTO | `src/VerusLLC.Application/Companies/CompanyDto.cs` |
| HTTP request/response contracts | `src/VerusLLC.Api/Contracts/Companies/` |
| Search ranking | `src/VerusLLC.Application/Companies/Queries/SearchCompanies/CompanySearchScoring.cs` |

`CompanyDto` is the Application-layer shape; `CompanyResponse` is the HTTP shape. The `Company` entity
is never serialized.

## Running locally

### Backend

From the **repository root**:

```bash
dotnet restore VerusLLC.slnx
dotnet run --project src/VerusLLC.Api
```

`launchSettings.json` defines two profiles:

| Profile | URLs |
| --- | --- |
| `https` (default) | `https://localhost:7151` and `http://localhost:5152` |
| `http` | `http://localhost:5152` |

```bash
dotnet run --project src/VerusLLC.Api --launch-profile http     # HTTP only
```

For HTTPS, trust the local development certificate once:

```bash
dotnet dev-certs https --trust
```

To pick the port explicitly and bypass the profiles:

```bash
ASPNETCORE_URLS=http://localhost:5152 dotnet run --project src/VerusLLC.Api --no-launch-profile
```

In Development the API also serves OpenAPI at `/openapi/v1.json` and the Scalar UI at `/scalar/v1`
(`/scalar` redirects there).
`GET /` and `GET /health` are liveness endpoints that return `200`.

### Frontend

From `src/VerusLLC.App`:

```bash
npm install          # first time only
npm start            # ng serve on http://localhost:4200
```

The dev server proxies `/api` to the backend, so the browser only ever talks to its own origin and
**no CORS configuration is required**. The mapping is in `src/VerusLLC.App/proxy.conf.json`:

```json
{ "/api": { "target": "http://localhost:5152", "secure": false, "changeOrigin": true } }
```

Start the backend with the `http` profile (or on port 5152) before the frontend, or change the proxy
target to match. The base URL the client uses is **not** hard-coded in components: it is
`environment.apiBaseUrl` (`src/environments/environment*.ts`), injected through the `API_BASE_URL`
token in `src/app/app.config.ts`. It defaults to the relative path `/api` in every environment.

Other frontend scripts:

```bash
npm run build        # production build into dist/VerusLLC.App/browser
npm test             # vitest (watch mode)
npx ng test --watch=false   # single run
```

## Running with Docker Compose

This is the fastest way to see the whole thing working. **Neither the .NET SDK nor Node is needed on
the host** — only Docker.

### Requirements

- Docker Engine or Docker Desktop, running.
- Docker Compose v2 (`docker compose`, not the old `docker-compose`).

Run every command below from the **repository root**.

### Access points

| What | URL |
| --- | --- |
| Frontend | <http://localhost:4200> |
| API (direct, for Postman/curl) | <http://localhost:5000/api/companies> |
| Health | <http://localhost:5000/health> |

### Commands

```bash
# Validate the configuration
docker compose config

# Build and start in the foreground
docker compose up --build

# Or in the background
docker compose up --build -d

# Status and logs
docker compose ps
docker compose logs -f api frontend

# Stop and remove the containers and the project network
docker compose down
```

In the foreground, **Ctrl+C** stops the stack. Logs go to stdout/stderr, so `docker compose logs` is
the only place to read them; no log files are written.

### Checking it through the proxy

These go through the frontend origin, so they exercise Nginx as well as the API:

```bash
curl -i -X POST http://localhost:4200/api/companies \
  -H "Content-Type: application/json" \
  -d '{"name":"Microsoft","websiteUrl":"https://www.microsoft.com"}'

# 201 Created, Location: /api/Companies/<id>
curl -i http://localhost:4200/api/companies/<id>
curl "http://localhost:4200/api/companies?search=micro"
```

The `Location` header is **relative**, so it is always reachable from the browser and never leaks the
internal `api` hostname.

### How the two services talk

| Port | Meaning |
| --- | --- |
| `4200` (host) → `8080` (frontend container) | Nginx serving the Angular build |
| `5000` (host) → `8080` (api container) | Kestrel, for direct API access |
| `api:8080` | Internal only — used by Nginx, never by the browser |

The browser only ever calls `/api/...` on its own origin (`localhost:4200`). Nginx forwards those to
`http://api:8080` over the Compose network, preserving the full `/api/...` path. Because the browser
never makes a cross-origin request, **no CORS configuration is involved**. Everything is plain HTTP in
this setup; the HTTPS profile is for local `dotnet run` only, and no certificates are baked into the
images.

The frontend is a **static production build** served by Nginx — there is no hot reload. After changing
source, rebuild:

```bash
docker compose up --build -d
```

### Sample data

The API seeds 8 sample companies at startup (Microsoft, GitHub, Shopify, Atlassian, Contoso,
Fabrikam, Acme Labs, Northwind Traders) so the first page load is not an empty list. Seeding only runs
when the store is empty. Turn it off with configuration:

```bash
Companies__SeedSampleData=false dotnet run --project src/VerusLLC.Api
```

or in `compose.yaml` under the `api` service:

```yaml
environment:
  Companies__SeedSampleData: "false"
```

### Data lifetime

Companies live in the API process's memory. There are no volumes and no database, so
`docker compose down`, `docker compose restart api`, or any recreation of the API container **empties
the store**. The frontend keeps working and can create companies again as soon as the API is healthy
again — Nginx re-resolves the `api` hostname, so it does not need restarting.

### Ports already in use

If 4200 or 5000 are taken, copy `.env.example` to `.env` and change them:

```bash
cp .env.example .env
# FRONTEND_PORT=4300
# API_PORT=5050
docker compose up --build -d
```

Only the **host** side changes; the containers keep listening on 8080 internally, and the frontend
still reaches the API at `api:8080`.

### Startup order

`frontend` waits for `api` to report healthy — Compose uses `condition: service_healthy` against a
real `GET /health` check inside the API container, not a fixed sleep.

Running in containers does not replace the test suite. `dotnet test` and `ng test` below still apply.

## Tests and build

```bash
# Backend — from the repository root
dotnet build VerusLLC.slnx          # 0 errors, 0 warnings
dotnet test VerusLLC.slnx           # 94 tests

# Frontend — from src/VerusLLC.App
npx ng test --watch=false           # 13 tests
npm run build
```

`global.json` opts `dotnet test` into the Microsoft.Testing.Platform runner. xUnit v3 requires it on
the .NET 10 SDK — without that file `dotnet test` refuses to run at all. Don't delete it.

## Continuous integration

`.github/workflows/ci.yml` runs on every push and pull request to `main`, and can be started manually
from the Actions tab. Three jobs run in parallel:

| Job | What it does |
| --- | --- |
| **Backend (.NET)** | `dotnet restore`, `build --configuration Release -warnaserror`, `dotnet test` |
| **Frontend (Angular)** | `npm ci`, `ng test --watch=false`, `npm run build` |
| **Docker Compose** | `docker compose config`, builds both images, starts the stack and smoke-tests it |

The backend job treats **any warning as an error**, so the 0-warning baseline cannot silently
regress. NuGet and npm caches are keyed off the lockfiles.

The Docker job is the end-to-end gate. It starts the real stack with `docker compose up -d --wait`
and then checks, through the frontend proxy exactly as a browser would: `/health` answers, the seeded
list is non-empty, a create returns `201` with a **relative** `Location` that resolves, an invalid
name/host pair returns `400`, an unknown id returns a JSON `404` rather than the SPA fallback, an
empty search returns `[]`, and an Angular deep link still serves the app. On failure it dumps
`docker compose logs`, and it always tears the stack down.

## API reference

Base path `/api/companies`. Examples below use the HTTP profile (`http://localhost:5152`); through the
Angular dev server, replace the origin with `http://localhost:4200`.

### Create a company

```bash
curl -i -X POST http://localhost:5152/api/companies \
  -H "Content-Type: application/json" \
  -d '{"name":"Microsoft","websiteUrl":"https://www.microsoft.com"}'
```

`201 Created`, with a **relative** `Location` header so it survives any reverse proxy:

```
HTTP/1.1 201 Created
Location: /api/Companies/01a0c404-c9bc-7bc9-a0ad-6624f3e88d05

{"id":"01a0c404-c9bc-7bc9-a0ad-6624f3e88d05","name":"Microsoft","websiteUrl":"https://www.microsoft.com"}
```

The server always mints the id. `CreateCompanyRequest` has no `id` field, and one sent in the body is
ignored.

A rejected relationship returns `400` with `ProblemDetails` and stores nothing:

```bash
curl -i -X POST http://localhost:5152/api/companies \
  -H "Content-Type: application/json" \
  -d '{"name":"Microsoft","websiteUrl":"https://google.com"}'
```

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "company.name_host.mismatch",
  "status": 400,
  "detail": "Company name is not related to the host of the website URL.",
  "correlationId": "01a0c404-e3a3-72b5-8df7-5e4921f4c09e"
}
```

### Get one company

```bash
curl -i http://localhost:5152/api/companies/01a0c404-c9bc-7bc9-a0ad-6624f3e88d05
```

| Case | Status |
| --- | --- |
| Found | `200` + `CompanyResponse` |
| Malformed id (`not-a-guid`) | `400` `ValidationProblemDetails` |
| `00000000-0000-0000-0000-000000000000` | `400` `company.id.required` |
| Well-formed but unknown | `404` `company.not_found` |

### Search

```bash
curl "http://localhost:5152/api/companies"                          # 200, everything
curl "http://localhost:5152/api/companies?name=micro"               # 200, name contains
curl "http://localhost:5152/api/companies?domain=microsoft.com"     # 200, exact host
curl "http://localhost:5152/api/companies?search=micro"             # 200, ranked
curl "http://localhost:5152/api/companies?name=contoso&domain=microsoft.com"  # 200, []
```

An empty result is `200` with `[]`, never `404`. A filter longer than 200 characters is `400`.

Every error response — whether it came from a domain rule or from model binding — carries the same
`correlationId` extension, echoed in the `X-Correlation-Id` response header. Send your own with the
request header of the same name and it is preserved.

## Business rules

### Name

Trimmed, internal whitespace collapsed, original casing preserved. **At least 3 characters** after
normalization, at most 100. `"  Acme   Labs  "` is stored as `"Acme Labs"`.

### Website URL

Must be an **absolute** `http` or `https` URL whose host contains a dot. The stored value is
normalized: scheme and host lowercased, default port dropped, a trailing `/` removed when there is no
path.

| Input | Result |
| --- | --- |
| `HTTPS://WWW.CONTOSO.COM/` | `https://www.contoso.com` |
| `https://contoso.com:8443/about` | kept as-is |
| `contoso.com` | rejected — not absolute |
| `ftp://contoso.com` | rejected — not http/https |
| `https://localhost` | rejected — host has no dot |

### Name–host relevance

The rule that makes a company acceptable:

1. Drop trailing legal suffixes from the name (`Inc`, `LLC`, `Ltd`, `Corp`, `GmbH`, `SA`, …).
2. Drop a leading `www.` from the host, then remove the public suffix
   (`microsoft.com` → `microsoft`, `contoso.co.uk` → `contoso`).
3. Reduce both to lowercase letters and digits, stripping accents and punctuation.
4. Accept only if the two are **exactly equal**.

| Name | Website | Result |
| --- | --- | --- |
| `Microsoft` | `https://microsoft.com` | accepted |
| `Microsoft Corporation` | `https://microsoft.com` | accepted — suffix dropped |
| `Microsoft` | `https://www.microsoft.com` | accepted — `www.` dropped |
| `Acme Labs` | `https://acmelabs.com` | accepted — spaces removed |
| `Ácme Labs` | `https://acmelabs.com` | accepted — accents stripped |
| `Microsoft` | `https://contoso.com` | rejected |
| `Microsoft` | `https://notmicrosoft.com` | rejected |
| `Microsoft` | `https://portal.microsoft.com` | rejected — see limitations |

### Search: filters and ranking

Filters are **ANDed** — a company must satisfy every supplied filter.

| Parameter | Match |
| --- | --- |
| `name` | substring of the company name |
| `domain` | **exact** host, ignoring a leading `www.` |
| `search` | substring of either the name or the host |

Comparison is case-insensitive, accent-insensitive and whitespace-collapsed. **Empty or whitespace-only
parameters are treated as absent**, so `?name=&search=` returns everything.

Each supplied term scores against the candidate, and the scores are summed:

| Match | Score |
| --- | --- |
| Name equals the term | 100 |
| Name starts with the term | 80 |
| Name contains the term | 60 |
| Host equals the term | 50 |
| Host starts with the term | 40 |
| Host contains the term | 20 |

Results are ordered by **score descending, then name (ordinal), then id**. The last two keys make the
order fully deterministic — repeating a query returns the same sequence. Anything scoring 0 is
excluded, so a search that matches nothing returns `[]` rather than the whole list.

For example, with `?search=microsoft`:

```
Microsoft      (exact name, 100)
Microsoftware  (name prefix, 80)
Microsoftworks (name prefix, 80 — after Microsoftware by name)
Notmicrosoft   (name contains, 60)
```

## Limitations

These are deliberate for this exercise, not oversights.

- **The relevance rule is a string heuristic. It does not prove ownership of the site.** Nothing is
  fetched, no DNS is resolved, no WHOIS is consulted. `Contoso` + `https://contoso.com` is accepted
  because the strings line up, not because the company owns the domain.
- **Subdomains are rejected.** `portal.microsoft.com` reduces to `portal.microsoft`, which is not equal
  to `microsoft`. This is the cost of requiring exact equality — the same strictness is what rejects
  `notmicrosoft.com`. Loosening it to substring matching would let impostor domains through.
- **Data is in memory and disappears when the API restarts.** There is no database, no file, no volume.
- **The sample data is seeded on every start**, so a restart brings the 8 samples back but loses
  anything you added.
- **There is no duplicate rule.** The same name and URL can be registered repeatedly, each time with a
  new id. Nothing enforces uniqueness.
- **There is no authentication or authorization.** Every endpoint is anonymous.
- CORS and rate limiting are configured in `Program.cs` but **not** applied to the pipeline; the proxy
  makes CORS unnecessary in both supported run modes.

## Extending it

**Replacing the in-memory store.** `ICompanyRepository`
(`src/VerusLLC.Application/Common/Persistence/`) is the only seam. Add an EF Core implementation in
`VerusLLC.Persistence`, register it in `AddPersistence()` instead of `InMemoryCompanyRepository`, and
nothing in Domain, Application or the HTTP contract changes. `Company` already keeps a private
constructor and no public setters, which EF Core can map to. The packages
(`Microsoft.EntityFrameworkCore`, `Npgsql` via `Testcontainers.PostgreSql` in tests) are already
pinned in `Directory.Packages.props`.

**Moving search into the database.** `SearchCompaniesQueryHandler` currently pulls a full snapshot with
`GetAllAsync` and ranks in memory, which is fine at this size and keeps the ranking unit-testable. With
a real store, push filtering behind the repository — add a method that takes the filters — so the
database does the work instead of loading every row. The ranking itself can stay in
`CompanySearchScoring` or move to SQL, but keep one implementation.

**Adding operations.** Update and delete follow the existing slice pattern: a request record under
`Companies/Commands/<UseCase>/`, an `internal sealed` handler returning `Result<T>`, a controller
action that translates failures through `ToProblem`. Uniqueness, if you want it, belongs in Domain
next to the other invariants, surfaced as a new `CompanyErrors` entry mapped to `ErrorType.Conflict`
(the `409` mapping already exists).

*None of the above is implemented — it is the intended path, not pending work in the codebase.*

## What is in the delivery

Sources, project files, configuration and the npm lockfile are included. Generated directories
(`bin/`, `obj/`, `node_modules/`, `dist/`) are excluded by `.gitignore`, and there are no secrets or
credentials in the repository — the API has no connection strings and no API keys.

> Note: the Visual Studio `.gitignore` template ignores `*.app`, which would have excluded the whole
> `src/VerusLLC.App` directory. An explicit `!src/VerusLLC.App/` rule at the end of `.gitignore`
> restores it.
