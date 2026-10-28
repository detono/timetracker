# TimeTracker — Employee Time Tracking

[![Docker Version](https://img.shields.io/docker/v/detono/timetracker-api?style=flat-square)](https://hub.docker.com/r/detono/timetracker-api)
[![Docker Pulls](https://img.shields.io/docker/pulls/detono/timetracker-api?style=flat-square)](https://hub.docker.com/r/detono/timetracker-api)
[![Image Size](https://img.shields.io/docker/image-size/detono/timetracker-api/latest?style=flat-square)](https://hub.docker.com/r/detono/timetracker-api)

[![Docker Version](https://img.shields.io/docker/v/detono/timetracker-frontend?style=flat-square)](https://hub.docker.com/r/detono/timetracker-frontend)
[![Docker Pulls](https://img.shields.io/docker/pulls/detono/timetracker-frontend?style=flat-square)](https://hub.docker.com/r/detono/timetracker-frontend)
[![Image Size](https://img.shields.io/docker/image-size/detono/timetracker-frontend/latest?style=flat-square)](https://hub.docker.com/r/detono/timetracker-frontend)

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

### Companies (tenants)

One installation can host several companies. Each company (tenant) has its own employers,
employees, hour types, projects and time entries, and can never see another company's
data. This is enforced centrally in `ApplicationDbContext`: every query on company data is
filtered to the signed-in user's tenant, and every write is checked against it.

A **platform admin** belongs to no company. They create companies (together with each
company's first employer account, plus default hour types), deactivate or reactivate
them, and manage other platform admins. They cannot see any company's hours or people.
The first platform admin is created from `.env` (see "Configuration" below). Email
addresses are unique across the whole installation, so login needs only email and password.

### Managing accounts

There's no public sign-up: creating accounts is an Employer-only action, both in the API
(`POST /api/users` requires the `Employer` role) and in the UI (an Employer sees a **"Manage
employees"** page to create accounts, deactivate leavers, reactivate them, and assign
supervisors). To try it, log in as the bootstrap platform admin, create a company, then
log in with that company's employer account. Deactivating a
user is a soft delete — their historical time entries and report data are kept; they simply
can no longer log in.

### Leave

- **Work schedules.** Working hours are set per weekday (start, end and break). An employer
  sets the company's usual hours under *Manage employees → Company work schedule*;
  everyone follows those unless they get their own schedule (*Manage employees →
  Schedule*), e.g. a part-timer. *Use company schedule* puts someone back on the
  company's hours. Without a company schedule, Monday to Friday, 9:00–17:00 with a
  30-minute break applies. Changing a schedule doesn't change leave that's already booked.
- **Requests.** Anyone can request leave for a date range under an hour type marked *can be
  requested as leave*. The form shows how many working days and hours that is, and what's
  left of their balance.
- **Approval.** The requester's supervisor, or any employer, approves or rejects it. Approving
  books one time entry per working day with that person's scheduled hours, so the leave
  shows up in the planning. Revoking approved leave removes those entries again.
- **Balances.** Employers set a yearly allowance in hours per person (*Leave → Leave
  balances*). Hours logged under types marked *uses up the leave balance* count as used.
  Going over the balance shows a warning to the requester and the approver but doesn't block
  approval.
- **Notifications.** Approvers see a count next to *Leave* and a banner on their dashboard.
  If the company's email is set up, the supervisor (or every employer, when there's no
  supervisor) gets an email about new requests, and the requester gets one when it's
  decided. Emails are in English for now.

### Worked vs. expected

*My hours* shows this week, this month and the year so far against your work schedule,
plus the week day by day. Employers (everyone) and supervisors (their team) see the same per
person on the team overview. Nothing is stored: the numbers are recalculated from logged hours
and schedules every time, so there's no balance to manage or correct.

- Expected hours count up to yesterday, plus today once something is logged for it, so you
  don't look behind every morning before you've logged the day.
- Every logged hour counts as worked, including leave and sick days.
- Expected hours start on the day someone's account was created.
- The current schedule applies to the whole period; earlier schedule changes aren't remembered.
- People who don't log hours (e.g. an owner) can be switched off under *Manage employees →
  Tracks hours*; they're then left out of the overview.

The app follows the operating system's light or dark mode.

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
- `SEED_ADMIN_EMAIL` / `SEED_ADMIN_PASSWORD` — the bootstrap platform admin, see below

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
      Seed__AdminEmail: "${SEED_ADMIN_EMAIL:-}"
      Seed__AdminPassword: "${SEED_ADMIN_PASSWORD:-}"
      Seed__AdminFirstName: "${SEED_ADMIN_FIRST_NAME:-}"
      Seed__AdminLastName: "${SEED_ADMIN_LAST_NAME:-}"
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

### Outgoing email (per company)

Each company sends email with its own settings, which an employer manages on the
**Email** page (`GET`/`PUT /api/mail-settings`, `POST /api/mail-settings/test`). Nothing
goes in `.env`. If a company has no settings, or has "Send email" switched off, the app
just skips sending and logs it — it's never an error. **Send test email** mails the
signed-in employer with the saved settings (also while sending is switched off) and shows
why it failed if it did.

The SMTP password and Graph client secret are encrypted with ASP.NET Core Data Protection
before they're stored, and the API never returns them (only whether one is set; leave the
field empty to keep it). The Data Protection key ring is stored in the same database
(`DataProtectionKeys` table), so it survives container restarts. Keep in mind that a full
database backup therefore contains both the keys and the encrypted secrets: protect
backups like you'd protect the secrets themselves. If the keys are ever lost, the app asks
to re-enter the secret.

For code: inject `IEmailSender` and call `SendAsync(new EmailMessage(to, subject, html, text))`.
It uses the current user's company settings and returns `Sent`, `NotConfigured` or
`Failed` (with the reason, also logged) instead of throwing.

**SMTP (e.g. Brevo)**

1. In Brevo, add and verify your sending domain, then add the SPF, DKIM (and ideally
   DMARC) DNS records Brevo shows you. Without them, mail from your domain is likely to
   land in spam or be rejected.
2. Under *SMTP & API → SMTP*, copy the SMTP login and generate an SMTP key.
3. On the Email page choose *SMTP server*: host `smtp-relay.brevo.com`, port `587`,
   security *STARTTLS*, username = the SMTP login, password = the SMTP key, sender
   address = an address on the verified domain.

Any other SMTP provider works the same way. Use *SSL/TLS* for port 465. Microsoft 365
usually has password-based SMTP turned off — use Microsoft Graph for it instead.

**Microsoft 365 (Microsoft Graph)**

1. In the Microsoft Entra admin center, *App registrations → New registration* (single
   tenant, no redirect URI). Note the *Application (client) ID* and *Directory (tenant) ID*.
2. *Certificates & secrets → New client secret*. Copy the value now; note when it expires
   and renew it before then.
3. *API permissions → Add → Microsoft Graph → Application permissions → `Mail.Send`*, then
   *Grant admin consent*. It must be the **Application** permission, not Delegated.
4. Restrict the app to the one mailbox it should send from. Without this, `Mail.Send`
   lets the app send as **any** mailbox in the organization. In Exchange Online
   PowerShell, either use RBAC for Applications (recommended), or the older application
   access policy:

   ```powershell
   New-ApplicationAccessPolicy -AppId <client-id> -PolicyScopeGroupId noreply@company.com `
     -AccessRight RestrictAccess -Description "TimeTracker may only send as noreply"
   ```

5. On the Email page choose *Microsoft 365*, fill in the tenant id, client id, secret and
   the mailbox (e.g. `noreply@company.com`, a shared mailbox works too), and use the same
   address as sender address. A different sender address needs *Send As* rights on it.

A 401/403 from the test email almost always means the permission is missing, is
Delegated instead of Application, lacks admin consent, or the access policy doesn't
include the mailbox. Policy changes can take up to an hour to apply.

## License

[Mozilla Public License 2.0](LICENSE) (MPL-2.0) — a file-level copyleft license: you're free
to use, modify, and self-host this (including commercially), but changes to MPL-covered
files must themselves stay under MPL-2.0 if you distribute them. You *can* combine this
code with proprietary code in a larger work, as long as the MPL-covered files stay in their
own files under MPL-2.0. See the [LICENSE](LICENSE) file for the full text.