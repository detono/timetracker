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

```yaml
services:
  db:
    image: postgres:16-alpine
    container_name: timetracker-db
    restart: unless-stopped
    environment:
      POSTGRES_DB: timetracker
      POSTGRES_USER: "${DB_USER:-timetracker}"
      POSTGRES_PASSWORD: "${DB_PASSWORD:?Set DB_PASSWORD in a .env file - see .env.example}"
    volumes:
      - timetracker-db-data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U ${DB_USER:-timetracker} -d timetracker"]
      interval: 5s
      timeout: 5s
      retries: 10

  api:
    image: detono/timetracker-api:latest
    container_name: timetracker-api
    restart: unless-stopped
    depends_on:
      db:
        condition: service_healthy
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      ConnectionStrings__DefaultConnection: "Host=db;Port=5432;Database=timetracker;Username=${DB_USER:-timetracker};Password=${DB_PASSWORD:?Set DB_PASSWORD in a .env file - see .env.example}"
      Jwt__Secret: "${JWT_SECRET:?Set JWT_SECRET in a .env file - see .env.example}"
      Jwt__Issuer: "TimeTracker.API"
      Jwt__Audience: "TimeTracker.Client"
      Cors__AllowedOrigins__0: "http://localhost:8080"
      Seed__EmployerEmail: "${SEED_EMPLOYER_EMAIL:-}"
      Seed__EmployerPassword: "${SEED_EMPLOYER_PASSWORD:-}"
      Seed__EmployerFirstName: "${SEED_EMPLOYER_FIRST_NAME:-}"
      Seed__EmployerLastName: "${SEED_EMPLOYER_LAST_NAME:-}"
    ports:
      - "5067:8080"

  frontend:
    image: detono/timetracker-frontend:latest
    container_name: timetracker-frontend
    restart: unless-stopped
    depends_on:
      - api
    environment:
      # Rendered into config.js at container start (docker-entrypoint.sh), not baked into
      # the build - change these and just restart (no rebuild needed) to reuse this same
      # image for a different client/brand. Quote hex values in .env, e.g. COLOR_PRIMARY="#932e4a",
      # or the "#" can be misread as a comment by some .env parsers.
      APP_TITLE: "${APP_TITLE:-TimeTracker}"
      COLOR_PRIMARY: "${COLOR_PRIMARY:-#932e4a}"
      COLOR_ON_PRIMARY: "${COLOR_ON_PRIMARY:-#ffffff}"
      COLOR_SECONDARY: "${COLOR_SECONDARY:-#e4d1dc}"
      COLOR_ON_SECONDARY: "${COLOR_ON_SECONDARY:-#000000}"
    ports:
      - "8154:80"

volumes:
  timetracker-db-data:
```

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

## License

[Mozilla Public License 2.0](LICENSE) (MPL-2.0) — a file-level copyleft license: you're free
to use, modify, and self-host this (including commercially), but changes to MPL-covered
files must themselves stay under MPL-2.0 if you distribute them. You *can* combine this
code with proprietary code in a larger work, as long as the MPL-covered files stay in their
own files under MPL-2.0. See the [LICENSE](LICENSE) file for the full text.