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

### Passwords and signing in

- **Temporary passwords.** A password someone else chose is temporary: for a new account
  (created by an employer or a platform admin), or after a reset. After signing in with it,
  the person must choose their own before they can do anything else. The API enforces this,
  not only the UI.
- **Resets.** An employer resets an employee's password under *Manage employees*. A platform
  admin resets an employer's password under *Companies → Reset employer password*, for when
  a company's employer is locked out. Platform admins can't reset employees.
- **Forgot password.** If a company has outgoing email set up, its people can reset their own
  password from the login page: they get an emailed code (not a link) that works for one hour
  and allows five wrong tries, and at most one code is sent per minute. The login page only
  offers this when some company on the installation has email switched on; otherwise it
  says to ask your employer. Platform admins have no company email, so another platform
  admin has to help them.
- **Signing out.** Deactivating a person or a company, resetting a password or changing it
  signs that person out everywhere on their next click, not only when their sign-in
  expires. Changing your own password keeps you signed in on the device you used.
- **Mobile app sign-in.** The mobile app asks for a refresh token when it signs in, so people
  don't type their password every day. Only a hash of it is stored. Each one works once and is
  swapped for a new one; one that isn't used for `JWT_REFRESH_TOKEN_DAYS` days (default 14)
  stops working. If an already-used token shows up again (after a short grace period for
  retries on a bad connection), someone copied it, and that whole sign-in is ended. Signing
  out in the app, changing or resetting the password, or deactivating the person or their
  company ends it as well. The website doesn't use refresh tokens.
- **Password guessing.** Five wrong passwords for one email address within 15 minutes block
  signing in with it for 15 minutes; resetting the password lifts the block. This works the
  same for addresses without an account, so it doesn't reveal which ones exist. On top of
  that, one IP address can make 20 sign-in or reset requests a minute
  (`RateLimiting__AuthRequestsPerMinute`; raise it if a big office shares one IP). The counts
  are kept in memory, so a restart clears them.
- **Behind a reverse proxy.** The API takes the client's IP from `X-Forwarded-For`, but only
  when the request comes from a private network (like the bundled nginx, or Traefik or Caddy
  on the same Docker network). Requests straight from the internet can't fake their address.

### Leave

- **Work schedules.** Working hours are set per weekday (start, end and break). An employer
  sets the company's usual hours on the *Company* page;
  everyone follows those unless they get their own schedule (*Manage employees →
  Schedule*), e.g. a part-timer. *Use company schedule* puts someone back on the
  company's hours. Without a company schedule, Monday to Friday, 9:00–17:00 with a
  30-minute break applies. Changing a schedule doesn't change leave that's already booked.
- **Requests.** Employees request leave for a date range under an hour type marked *can be
  requested as leave*. The form shows how many working days and hours that is, and what's
  left of their balance.
- **Half days.** A single day can be the whole day, the morning or the afternoon; over
  several days, the first day can start in the afternoon and the last day end at noon. A
  half day is half of that day's scheduled hours, without a break (on 9:00–17:00 with 30
  minutes' break: 9:00–12:45 or 13:15–17:00).
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
  decided. Emails are in each recipient's language (see "Languages") and use the app's name
  and colours (`APP_TITLE`, `COLOR_*`).

### Closed months

Once a month's hours are final (e.g. after payroll), an employer locks it under *Reports →
Closed months*. Nobody can then add, change, move or delete hours in that month, employers
included, and leave that touches it can't be requested, approved or revoked. A pending
request can still be rejected or withdrawn, since that changes no hours. The app shows
locked hours with a lock and explains why; the API enforces it too. To fix something, an
employer unlocks the month, makes the change and locks it again. Months that haven't
started can't be locked.

### Activity log

The *Company* page shows employers who did what: locking and unlocking months, adding,
changing or removing someone else's hours (with the hours before and after), password resets
(also by a platform admin), (de)activated accounts and added, changed or removed webhooks. Entries can't be changed or removed,
and they're kept with the company's data.

### Company settings

Employers manage company-wide settings on the *Company* page: name and address, the usual
working hours, public holidays, outgoing email and webhooks.

**Public holidays.** The company's country (and, where holidays differ per region, a region
like `DE-BY`) decides which official holidays apply. *Load official holidays* fetches them
for a year from [Nager.Date](https://date.nager.at), a free service with no account or key;
set `PublicHolidays__BaseUrl` to use another instance. The holidays are stored with the
company, so an employer can remove days the company works anyway or add its own days off.
Nobody is expected to work on them, and leave over them doesn't use up the balance.

### Projects

Employers manage projects on the *Projects* page. Besides a name and an optional client, a
project can have:

- **A budget** in hours. The list shows how much of it is used, e.g. "70% of 120h", in red
  once it goes over. Every hour logged on the project counts, whenever it was logged.
- **Billable**: whether its hours are charged to the client, so reports can tell them apart.
- **Who can log hours on it**: everyone (the default), or only the people chosen. The others
  don't see it when they log hours, and the API refuses it for them. Someone who's taken off a
  project can still correct the hours they logged on it before.

*View details* shows the hours per person, over all time or between two dates.

*Reports* can be limited to one project (the hours report and *Download all entries (CSV)*;
worked vs. expected stays about all hours). It also shows the hours per project in the dates,
with how many are billable and a line for hours without a project, and
*Download hours per project (CSV)* exports that table.

### Worked vs. expected

*My hours* shows this week, this month and the year so far against your work schedule,
plus the week day by day. Employers (everyone) and supervisors (their team) see the same per
person on the team overview. Nothing is stored: the numbers are recalculated from logged hours
and schedules every time, so there's no balance to manage or correct.

- Expected hours count up to yesterday, plus today once something is logged for it, so you
  don't look behind every morning before you've logged the day.
- Every logged hour counts as worked, including leave and sick days.
- Expected hours start on the day someone's account was created, or on the first day hours
  were logged for them if that's earlier.
- Public holidays and company days off aren't expected work.
- The current schedule applies to the whole period; earlier schedule changes aren't remembered.
- People who don't log hours (e.g. an owner) can be switched off under *Manage employees →
  Tracks hours*; they're then left out of the overview.

*Reports* shows the same comparison per person per day, week or month for any dates, and
*Download worked vs. expected (CSV)* exports it. *Download all entries (CSV)* exports every
entry in the dates instead, one row each: date, start and end time, break, hours, hour type,
project, location, notes, and when it was recorded and last changed (UTC). That's the
working-time record to hand over when someone asks when people worked, not only how much.
Like the reports, employers get everyone, others themself and their team. Names in CSV files
are made safe for spreadsheets, so a name like `=HYPERLINK(...)` isn't run as a formula.

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

### External services

TimeTracker runs fully on your own server. It only contacts other services in these cases,
and none of them need an account or key from us:

| Service | Called by | When | What it receives |
| --- | --- | --- | --- |
| [Google Fonts](https://fonts.google.com) | The browser | Every page load | The visitor's IP address and browser details, as with any website font |
| [OpenStreetMap Nominatim](https://nominatim.org) | The browser | Only when someone clicks *Use my location* on a time entry | The coordinates, to turn them into a street address |
| [Nager.Date](https://date.nager.at) | The API | Only when an employer clicks *Load official holidays* | The country code and year |
| Your SMTP server or Microsoft 365 | The API | Only when the company's email is set up and switched on | The emails being sent |
| The company's webhook addresses | The API | Only for webhooks an employer added and switched on | The events they listen to (see [Webhooks](#webhooks-per-company)) |

Nothing else leaves your server: no analytics, tracking or update checks. Nager.Date can
point to your own instance with `PublicHolidays__BaseUrl`. If the location lookup fails,
the raw coordinates are kept instead.

### Server info

`GET /api/info` answers without signing in, with `{ "name": "TimeTracker", "apiVersion": 1 }`.
Apps (like a phone app) use it to check that an address really is a TimeTracker server and
that they can talk to it. `apiVersion` only goes up when the API changes in a way that breaks
existing apps.

## Configuration (do this first)

Copy the environment template and fill in real values:

```bash
cp .env.example .env
```

`.env` is gitignored — it's read automatically by `docker compose` (no flag needed). At minimum, set:

- `DB_PASSWORD` — Postgres password
- `JWT_SECRET` — any long random string (`openssl rand -base64 48` works well). The API
  refuses to start without one, with one shorter than 32 characters, or with the example value
- `SEED_ADMIN_EMAIL` / `SEED_ADMIN_PASSWORD` — the bootstrap platform admin, see below. The
  example password is refused, and you choose a new password at the first sign-in

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
      # Allow company email settings to use a mail server on an internal network (see README).
      Email__AllowInternalHosts: "${EMAIL_ALLOW_INTERNAL_HOSTS:-false}"
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

### Security

- **Use HTTPS.** Put a reverse proxy with TLS (Caddy, Traefik, nginx) in front of the frontend
  container, and turn on HSTS there. The app itself serves plain HTTP inside Docker.
- **Security headers.** The frontend sends a strict Content Security Policy (scripts only from
  this site, fonts from Google Fonts, location lookups to OpenStreetMap), and blocks framing
  and content sniffing. They're in `frontend/security-headers.conf`. If you build the frontend
  with `VITE_API_BASE_URL` on another origin, add that origin to `connect-src` there.
- **Company mail servers.** Company email settings may only use mail servers on the internet,
  so an employer can't make this server connect to services on its own network. If your mail
  server is on your network, set `EMAIL_ALLOW_INTERNAL_HOSTS=true` (`Email__AllowInternalHosts`).
- **Sign-ins** are checked on every request: deactivated people and companies are signed out
  right away, and a token can't claim a different role or company than its account has.
- **Backups** contain the encryption keys for mail secrets, so protect them like the secrets.
- **Personal data.** Whoever hosts an installation is responsible for the personal data in
  it (names, email addresses, hours, and locations when people share them), including privacy
  rules like the GDPR. TimeTracker keeps everything until you remove it.

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
**Company** page (`GET`/`PUT /api/mail-settings`, `POST /api/mail-settings/test`). Nothing
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

Emails use the installation's `APP_TITLE` as the app's name. Set `APP_URL` in `.env` to
the address people open the app at (e.g. `https://hours.example.com`) and leave request
emails get **Approve** and **Reject** buttons in the brand colours. They open the request
on the Leave page, where the approver signs in if needed and confirms; nothing is decided
from the email alone, so a mail scanner opening the links can't approve anything.

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

### Webhooks (per company)

A webhook tells another system (payroll, HR, or a tool like n8n, Zapier, Make or Power
Automate) when something happens here. An employer adds them under *Company → Webhooks*:
an address, the events to send, and optionally an extra header such as
`Authorization: Bearer …` for APIs that need a key. *Send test* sends a made-up event right
away and shows the answer. *Recent calls* lists the last calls and how they went.

**Events:** `leave.requested`, `leave.approved`, `leave.rejected` and `leave.cancelled`.

**External IDs.** Give employees (*Manage employees → ⋯ → External ID*) and leave types
(*Hour types → Edit*) the id they have in the other system, like a payroll number or an
absence code. They're sent along, so the receiver knows who and what an event is about.

Each event is a `POST` with a JSON body:

```json
{
  "id": "21ff0941-1378-42c0-b5d8-f7b17112e9e8",
  "event": "leave.approved",
  "occurredAtUtc": "2026-11-09T14:27:52Z",
  "actor": { "id": "…", "name": "Sam Lead" },
  "employee": { "id": "…", "externalId": "EMP-0042", "firstName": "Jonathan", "lastName": "Peeters", "email": "jonathan@acme.test" },
  "leave": {
    "id": "…", "status": "Approved",
    "type": { "id": "…", "externalId": "PTO", "names": { "en": "Paid time off", "nl": "Verlof" } },
    "startDate": "2026-11-16", "endDate": "2026-11-20", "firstDayPart": "Full", "lastDayPart": "Full",
    "hours": 37.5, "comment": "Family trip", "reviewComment": null,
    "days": [
      { "date": "2026-11-16", "startTime": "09:00:00", "endTime": "17:00:00" },
      { "date": "2026-11-17", "startTime": "09:00:00", "endTime": "17:00:00" }
    ]
  }
}
```

`days` lists the working days that were booked, each with its hours from the employee's work
schedule (local time; weekends and public holidays are left out, half days have their own
times). It's only on `leave.approved`, when they're booked; other events have `null`.

Every event has `id`, `event`, `occurredAtUtc`, `actor` and `employee`, plus a block for what
it's about (`leave` for now). New events add new blocks; existing fields don't change.

**Delivery.** Events are saved together with the change they're about and sent in the
background within a few seconds, so a slow or broken receiver never slows the app down. Any
`2xx` answer counts as delivered. Otherwise the call is tried again after 1 minute, 5 minutes,
30 minutes, 2 hours and 6 hours, then marked as failed; an employer can *Send again*. A `4xx`
(except `408` and `429`) isn't retried, since waiting won't fix it. Redirects aren't followed.
`id` stays the same on every retry, so use it to skip duplicates. Finished calls are kept 30 days.

**Checking that a call is genuine.** Each webhook has a signing secret (*Show* in its form).
Every call carries these headers:

| Header | Value |
| --- | --- |
| `X-Webhook-Event` | The event, e.g. `leave.approved` |
| `X-Webhook-Id` | The event's `id` |
| `X-Webhook-Delivery` | This call's id (differs per webhook) |
| `X-Webhook-Timestamp` | Unix time in seconds when it was sent |
| `X-Webhook-Signature` | `sha256=` + hex HMAC-SHA256 of `{timestamp}.{body}`, keyed with the secret |

Compute the HMAC over the raw body, compare in constant time, and reject old timestamps
(say, older than 5 minutes) so a captured call can't be replayed. In Node.js:

```js
const crypto = require("crypto");
const expected = "sha256=" + crypto.createHmac("sha256", secret)
  .update(`${req.headers["x-webhook-timestamp"]}.${rawBody}`).digest("hex");
const got = Buffer.from(req.headers["x-webhook-signature"] ?? "");
const ok = got.length === expected.length && crypto.timingSafeEqual(Buffer.from(expected), got);
```

**Network.** Like mail servers, webhooks may only call public addresses by default, so an
employer can't use them to reach services inside your server's network. If the receiver runs
on your own network (e.g. your own n8n), set `WEBHOOK_ALLOW_INTERNAL_HOSTS=true` in `.env`.
Secrets and header values are encrypted the same way as mail secrets.

For code: to add an event, add its name to `WebhookEvents`, call `WebhookQueue.QueueAsync`
before the handler's `SaveChanges`, and add a test.

## Languages

The app ships in English, Dutch and French. Everyone picks their own language under *My
account*; it's saved with their account and also used for the emails they get. Until they
pick one, the browser's language is used. Dates follow the chosen language too.

### Adding a language

A language is one file for the app and, optionally, one for the emails. Nothing else needs
changing: new files are picked up automatically and show up in the language picker.

1. Copy `frontend/src/locales/en.json` to `frontend/src/locales/<code>.json`, using the
   two-letter language code (e.g. `de.json` for German), and translate the texts.
2. Optionally copy `src/TimeTracker.Application/Resources/Emails/en.json` to the same name
   there and translate the email texts.
3. Keep placeholders such as `{{name}}` (app) and `{name}` (emails) as they are.
4. Run `npm run i18n:missing` in `frontend/` to see what's still untranslated, and `npm test`
   to check the files.

You don't have to translate everything at once: any text that's missing falls back to
English. Hour type names are translated per company on the *Hour types* page.

## License

[Mozilla Public License 2.0](LICENSE) (MPL-2.0) — a file-level copyleft license: you're free
to use, modify, and self-host this (including commercially), but changes to MPL-covered
files must themselves stay under MPL-2.0 if you distribute them. You *can* combine this
code with proprietary code in a larger work, as long as the MPL-covered files stay in their
own files under MPL-2.0. See the [LICENSE](LICENSE) file for the full text.