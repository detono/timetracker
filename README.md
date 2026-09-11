# Shiftlog — Employee Time Tracking

[![Docker Version](https://img.shields.io/docker/v/detono/timetracker-api?style=flat-square)](https://hub.docker.com/r/detono/timetracker-api)
[![Docker Pulls](https://img.shields.io/docker/pulls/detono/timetracker-api?style=flat-square)](https://hub.docker.com/r/detono/timetracker-api)
[![Image Size](https://img.shields.io/docker/image-size/detono/timetracker-api/latest?style=flat-square)](https://hub.docker.com/r/detono/timetracker-api)

[![Docker Version](https://img.shields.io/docker/v/detono/timetracker-frontend?style=flat-square)](https://hub.docker.com/r/detono/timetracker-api)
[![Docker Pulls](https://img.shields.io/docker/pulls/detono/timetracker-frontend?style=flat-square)](https://hub.docker.com/r/detono/timetracker-api)
[![Image Size](https://img.shields.io/docker/image-size/detono/timetracker-frontend/latest?style=flat-square)](https://hub.docker.com/r/detono/timetracker-api)

[![Build Status](https://img.shields.io/github/actions/workflow/status/detono/timetracker/ci-cd.yml?branch=main&style=flat-square)](https://github.com/detono/timetracker/actions)
[![Tests](https://img.shields.io/github/actions/workflow/status/detono/timetracker/ci-cd.yml?branch=main&label=tests&style=flat-square)](https://github.com/detono/timetracker/actions)
[![Codecov](https://img.shields.io/codecov/c/github/detono/timetracker?style=flat-square&logo=codecov)](https://app.codecov.io/gh/detono/timetracker)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square&logo=dotnet)](https://github.com/detono/timetracker)
[![C#](https://img.shields.io/badge/C%23-239120?style=flat-square&logo=csharp&logoColor=white)](https://github.com/detono/timetracker)
[![React](https://img.shields.io/badge/React-20232A?style=flat-square&logo=react&logoColor=61DAFB)](https://github.com/detono/timetracker)
[![TypeScript](https://img.shields.io/badge/TypeScript-007ACC?style=flat-square&logo=typescript&logoColor=white)](https://github.com/detono/timetracker)
![License](https://img.shields.io/github/license/detono/timetracker?style=flat-square)
[![Support Tono on Ko-fi](https://img.shields.io/badge/Support_Tono-Tea-BD8C5E?style=flat-square&logo=ko-fi&logoColor=white)](https://ko-fi.com/detono)

A full-stack time-tracking application: employees log their worked hours, employers get
a complete overview and can extract reports per day/week/month, and everyone can switch
between a **list** and a **planboard** view.

## Architecture

The API is a .NET 8 solution built strictly around **Clean Architecture**:

```
src/
  TimeTracker.Domain          <- Entities, enums, repository interfaces. Zero dependencies.
  TimeTracker.Application     <- Use cases (CQRS via MediatR), DTOs, validators, interfaces
                                  for anything infrastructure-y (ICurrentUserService,
                                  IPasswordHasher, ITokenService...). Depends only on Domain.
  TimeTracker.Infrastructure  <- EF Core + PostgreSQL, repositories, JWT/password hashing.
                                  Implements the interfaces defined in Application.
  TimeTracker.API             <- ASP.NET Core controllers, middleware, composition root.
tests/
  TimeTracker.Domain.UnitTests        <- Entity invariants (xUnit + FluentAssertions)
  TimeTracker.Application.UnitTests   <- Handler behaviour & authorization rules (Moq)
  TimeTracker.API.IntegrationTests    <- End-to-end HTTP tests (WebApplicationFactory)
frontend/                     <- React + TypeScript + Vite SPA
```

Dependencies only ever point inward (`API → Infrastructure/Application → Domain`), so the
Domain and Application layers have no knowledge of ASP.NET Core, EF Core, or PostgreSQL.
Every cross-cutting concern (persistence, hashing, tokens, "who is the current user")
is expressed as an interface in `Application/Common/Interfaces` and implemented in
`Infrastructure` or `API`, per the **Dependency Inversion Principle** — this is also what
makes the Application layer fully unit-testable with mocks, with no database required.

### Roles & authorization model

There are two account types, `Employee` and `Employer`:

- An **Employee** can log and view their own hours.
- An **Employee** can also be assigned as the **supervisor** of other employees
  (`User.SupervisorId`). A supervisor can view (but not edit) their supervisees' hours —
  this is the "necessary authority" mechanism from the brief, without introducing a third
  role.
- An **Employer** can see and manage everyone's hours and run reports across the company.

This is enforced in the domain itself (`User.CanViewHoursOf`) and re-checked in every
relevant Application handler — never left to the UI alone.

### Managing accounts

There's no public sign-up: creating accounts is an Employer-only action, both in the API
(`POST /api/users` requires the `Employer` role) and in the UI (an Employer sees a **"Manage
employees"** page to create accounts, deactivate leavers, reactivate them, and assign
supervisors). Log in with the bootstrap employer account you configure via `.env` (see
"Configuration" below) to try it. Deactivating a
user is a soft delete — their historical time entries and report data are kept; they simply
can no longer log in.

### Key design choices

- **CQRS with MediatR**: every use case is its own Command/Query + Handler, each easy to
  test in isolation.
- **Result pattern**: handlers return `Result`/`Result<T>` instead of throwing for
  expected failures (not found / forbidden / conflict), so controllers map these onto
  correct HTTP status codes without try/catch soup.
- **FluentValidation** pipeline behaviour validates every command/query before it reaches
  its handler.
- **Repository + Unit of Work** abstractions in Domain, implemented with EF Core in
  Infrastructure — swappable without touching Application or API.
- Passwords are hashed with PBKDF2/HMAC-SHA256 (100k iterations) — no plaintext, no
  external dependency required to run the sample.

## Database schema

This project doesn't use EF Core migrations — on startup, the API creates its schema
directly from the current model (`Database.EnsureCreatedAsync()`), so there's nothing to
generate or commit before your first run. **Trade-off**: this can only create a schema on
an empty database; it can't incrementally alter one that already holds real data if you
change an entity later. That's a fine trade for how most self-hosted instances of this
run, but if you need proper zero-downtime schema evolution against a database with
production data, switch to migrations:

```bash
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialCreate \
  --project src/TimeTracker.Infrastructure \
  --startup-project src/TimeTracker.API \
  --output-dir Persistence/Migrations
```

commit the generated `Persistence/Migrations` folder, and change the `EnsureCreatedAsync()`
call in `DbSeeder.cs` to `MigrateAsync()`.

## Configuration (do this first)

Copy the environment template and fill in real values:

```bash
cp .env.example .env
```

`.env` is gitignored — it's read automatically by `docker compose` (no flag needed). At minimum, set:

- `DB_PASSWORD` — Postgres password
- `JWT_SECRET` — any long random string (`openssl rand -base64 48` works well); compose will
  refuse to start without it
- `SEED_EMPLOYER_EMAIL` / `SEED_EMPLOYER_PASSWORD` — see below

### Your first account

There's no public sign-up — creating accounts is Employer-only, both in the API and the UI
(see "Managing accounts" above). So something has to exist to log in with the very first
time: on startup, if the database is completely empty, the API creates **exactly one**
Employer account from `SEED_EMPLOYER_EMAIL` / `SEED_EMPLOYER_PASSWORD` (and optionally
`SEED_EMPLOYER_FIRST_NAME` / `SEED_EMPLOYER_LAST_NAME`) in your `.env` file. No demo data,
no placeholder employees — just the one account you asked for. Log in with it, then use
**"Manage employees"** to create everyone else and change your own password from the
account page.

If you leave those two blank, the app starts with zero users and logs a warning — set them
and restart (`docker compose up --build api`) whenever you're ready.

### Branding (white-label)

The app's title and two brand colors (primary/on-primary, secondary/on-secondary) are
**runtime-configurable** — set via `APP_TITLE`, `COLOR_PRIMARY`, `COLOR_ON_PRIMARY`,
`COLOR_SECONDARY`, `COLOR_ON_SECONDARY` in `.env`. Unlike the API URL (which is compiled
into the JS bundle at build time), these are rendered into a small `config.js` by
`frontend/docker-entrypoint.sh` every time the container starts. That means the exact same
built frontend image can be reused for a different client or deployment just by changing
these values and restarting — `docker compose up -d`, no `--build` needed:

```bash
# re-skin without rebuilding
docker compose up -d frontend
```

Quote hex values in `.env` (e.g. `COLOR_PRIMARY="#932e4a"`) — an unquoted `#` can be
misread as a comment by some `.env` parsers.

## Running locally with Docker (recommended)

Requires Docker and Docker Compose, and the `.env` file from the section above.

```bash
docker compose up --build
```

This starts three containers:

| Service  | URL                          |
|----------|-------------------------------|
| Frontend | http://localhost:8080         |
| API      | http://localhost:5067/swagger |
| Postgres | localhost:5432                |

The frontend's own nginx reverse-proxies `/api/*` straight to the API container over the
internal Docker network — so the app itself only ever needs `http://localhost:8080`; the
API's port above is just there for convenience (hitting Swagger directly, debugging, etc).

On first boot the API automatically creates its schema from the current EF Core model and
seeds your bootstrap employer account (see above). No manual database setup is required.

## Running without Docker

### API

Requires the .NET 8 SDK and a PostgreSQL instance.

```bash
cd src/TimeTracker.API
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=timetracker;Username=timetracker;Password=changeme"
dotnet user-secrets set "Jwt:Secret" "some-long-random-development-secret"
dotnet user-secrets set "Seed:EmployerEmail" "owner@yourcompany.com"
dotnet user-secrets set "Seed:EmployerPassword" "some-strong-password"
dotnet run
```

The API listens on `http://localhost:5067` and serves Swagger UI at `/swagger` in
Development.

### Frontend

Requires Node.js 20+.

```bash
cd frontend
cp .env.example .env   # point VITE_API_BASE_URL at your API if not localhost:5067
npm install
npm run dev
```

The dev server runs on http://localhost:5173.

## Tests

```bash
# Backend: domain, application (mocked handlers), and API integration tests
dotnet test TimeTracker.sln

# Frontend: unit tests
cd frontend && npm install && npm run test -- --run
```

## CI/CD

`.github/workflows/ci-cd.yml`:

1. **backend-build-and-test** — restores, builds, and runs the full .NET test suite with
   coverage collection. Runs on every push/PR to `main`.
2. **frontend-build-and-test** — installs, lints, unit-tests, and builds the SPA. Runs on
   every push/PR to `main`.
3. **docker-publish** (after both jobs pass, push events only — never on PRs) — builds and
   pushes the API and frontend images to **Docker Hub**:
   - Push to `main` → updates the rolling `:latest` tag (plus a `:<commit-sha>` tag).
   - Push a tag like `v1.2.0` → also publishes `:1.2.0`, `:1.2`, and `:1` tags — a proper
     versioned release self-hosters can pin to instead of riding `:latest`.
4. **github-release** (tag pushes only) — creates a GitHub Release with auto-generated
   notes, linking the exact image tags published for that version.

Required repository secrets (Settings → Secrets and variables → Actions):

| Secret               | Value                                                              |
|----------------------|---------------------------------------------------------------------|
| `DOCKERHUB_USERNAME` | Your Docker Hub username/org                                       |
| `DOCKERHUB_TOKEN`    | A Docker Hub [access token](https://hub.docker.com/settings/security) (not your password) |

To cut a release: `git tag v1.0.0 && git push origin v1.0.0`.

## Self-hosting from the published images (no build required)

Once images exist on Docker Hub, anyone can run the app without cloning or building
anything, using `docker-compose.release.yml`:

```bash
curl -O https://raw.githubusercontent.com/detono/timetracker/main/docker-compose.release.yml
curl -O https://raw.githubusercontent.com/detono/timetracker/main/.env.example
cp .env.example .env   # fill in DB_PASSWORD, JWT_SECRET, SEED_EMPLOYER_*, branding, etc.
DOCKERHUB_NAMESPACE=<your-dockerhub-username> docker compose -f docker-compose.release.yml up -d
```

Pin a specific version instead of `latest` with `IMAGE_TAG=v1.0.0` alongside
`DOCKERHUB_NAMESPACE` above.

## License

[Mozilla Public License 2.0](LICENSE) (MPL-2.0) — a file-level copyleft license: you're free
to use, modify, and self-host this (including commercially), but changes to MPL-covered
files must themselves stay under MPL-2.0 if you distribute them. You *can* combine this
code with proprietary code in a larger work, as long as the MPL-covered files stay in their
own files under MPL-2.0. See the [LICENSE](LICENSE) file for the full text.

## Extending this project

- **Third role / finer-grained permissions**: the authorization check lives in one place
  (`User.CanViewHoursOf` + the handlers that call it), so adding e.g. a `Manager` tier or
  per-project access is a localized change.
- **Swap PostgreSQL** for SQL Server/MySQL: only `Infrastructure/DependencyInjection.cs`
  and the EF Core provider package need to change.
- **Swap the password hasher / JWT for ASP.NET Core Identity or an external IdP**: both
  are hidden behind `IPasswordHasher`/`ITokenService`, so nothing else changes.
- **Overtime rules, leave requests, approvals**: add new entities/handlers following the
  same Domain → Application → Infrastructure → API flow used for `TimeEntry`.
