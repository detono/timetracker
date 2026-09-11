# Shiftlog — Employee Time Tracking

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
supervisors). Log in as the seeded `employer@demo.local` account to try it. Deactivating a
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

## Database migrations

This repository intentionally does **not** ship pre-generated EF Core migration files,
since they must be generated with the exact EF Core tooling version you build with. Run
this once (requires the [EF Core CLI tools](https://learn.microsoft.com/ef/core/cli/dotnet),
`dotnet tool install --global dotnet-ef`), before your first `docker compose up --build`
or `dotnet run`:

```bash
dotnet ef migrations add InitialCreate \
  --project src/TimeTracker.Infrastructure \
  --startup-project src/TimeTracker.API \
  --output-dir Persistence/Migrations
```

Commit the generated `Persistence/Migrations` folder — from then on the API applies it
automatically on startup (`DbSeeder.SeedAsync` calls `Database.MigrateAsync()`), including
inside the Docker container, so nobody else on the team needs to repeat this step. If you
change any entity or configuration later, generate a follow-up migration the same way
with a new name (e.g. `AddOvertimeFlag`).

## Running locally with Docker (recommended)

Requires Docker and Docker Compose.

```bash
docker compose up --build
```

This starts three containers:

| Service  | URL                          |
|----------|-------------------------------|
| Frontend | http://localhost:8080         |
| API      | http://localhost:5067/swagger |
| Postgres | localhost:5432                |

On first boot the API automatically applies EF Core migrations and seeds three demo
accounts (see below). No manual database setup is required.

> **Before deploying to anything but your own machine**, set a strong `JWT_SECRET`
> environment variable (32+ random bytes) — see `docker-compose.yml`.

### Demo accounts

| Email                  | Password       | Role     | Notes                              |
|-------------------------|----------------|----------|-------------------------------------|
| `employer@demo.local`   | `Password123!` | Employer | Sees and reports on everyone        |
| `lead@demo.local`       | `Password123!` | Employee | Supervises `employee@demo.local`    |
| `employee@demo.local`   | `Password123!` | Employee | Own hours only                      |

## Running without Docker

### API

Requires the .NET 8 SDK and a PostgreSQL instance.

```bash
cd src/TimeTracker.API
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=timetracker;Username=timetracker;Password=changeme"
dotnet user-secrets set "Jwt:Secret" "some-long-random-development-secret"
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

`.github/workflows/ci-cd.yml` runs on every push/PR to `main`:

1. **backend-build-and-test** — restores, builds, and runs the full .NET test suite with
   coverage collection.
2. **frontend-build-and-test** — installs, lints, unit-tests, and builds the SPA.
3. **docker-publish** (main branch only, after both jobs pass) — builds and pushes the API
   and frontend Docker images to GitHub Container Registry (`ghcr.io`), tagged with the
   commit SHA and `latest`.

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
