# Changelog

All notable changes to TimeTracker are listed here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and versions follow
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

Database migrations run automatically when the API starts, unless a release says otherwise.

## [Unreleased]

### Changed

- **New look**: the menu moved to a sidebar with icons, with the employer pages grouped under
  *Manage* and your name and sign-out at the bottom. On laptops and phones it slides in from
  the menu button.
- *My hours* shows a progress bar under the week, month and year, and how much leave you have
  left.
- Table rows with several actions (employees, hour types, projects, companies, hours) have one
  "⋯" menu instead of a row of buttons.
- On phones, your hours, leave and the employee list show as cards instead of wide tables.
- The *Company* page is split into tabs: General, Working hours, Public holidays, Email and
  Activity. `/email` opens the Email tab.

### Fixed

- Logging hours proposed 9:00 to 17:00 every day; it now proposes your scheduled hours and break
  for the chosen day.
- Just after midnight, logging hours proposed yesterday's date.

## [1.0.0] - 2026-10-29

### Added

- **Outgoing email per company**, via SMTP or Microsoft 365 (Graph), with a test email.
  Secrets are encrypted and never returned by the API.
- **Leave**: requests with approval by the supervisor or any employer, yearly leave
  balances in hours, and half days. Approving books the leave as time entries. Approvers
  get a badge, a dashboard banner and an email.
- **Work schedules**: a company schedule per weekday, with personal schedules for e.g.
  part-timers.
- **Worked vs. expected**: *My hours* shows the week, month and year so far against the
  schedule. Employers and supervisors see the same for their team.
- **Company page**: name, address, work schedule, public holidays and email settings in
  one place.
- **Public holidays**: load the official holidays for the company's country (and region)
  from Nager.Date, or add and remove days by hand.
- **Tracks hours** setting per person, to leave e.g. an owner out of the overview.
- **Language per user**, picked on *My account* and also used for their emails. Adding a
  language is a matter of adding a file (see the README).
- Dark mode that follows the operating system.
- README section listing every external service the app contacts.
- **Forgot password**: people reset their own password with a code emailed by their
  company, when it has email set up.
- **Platform admins can reset an employer's password**, for when a company is locked out.
- **Closed months**: employers lock a month once its hours are final. Nobody can change
  hours or leave in it until it's unlocked (*Reports → Closed months*).
- **Protection against password guessing**: 5 wrong passwords lock an address for 15 minutes,
  and sign-in and reset requests are limited per IP address. No extra dependencies.
- **Worked vs. expected in reports**: per person per period on the Reports page, with its own
  CSV export.
- **Activity log** on the Company page: who locked months, changed someone else's hours,
  reset passwords or (de)activated accounts.

### Security

- The API refuses to start with a missing, short or example JWT signing secret.
- Every request checks that the token's role and company match the account, so a token
  signed with a leaked secret still can't claim more rights.
- The bootstrap platform admin can't use the example password, and chooses a new password at
  the first sign-in.
- Company SMTP settings can't point at internal addresses (`localhost`, private networks,
  cloud metadata), unless `EMAIL_ALLOW_INTERNAL_HOSTS=true`. **Check this when upgrading** if
  your mail server is on your own network.
- Security headers (Content Security Policy, no framing, no sniffing) on everything the
  frontend serves, and nginx no longer shows its version.
- Endpoints require sign-in unless they explicitly allow anonymous access.
- Signing in with an unknown address takes as long as with a known one.
- Updated frontend packages with known vulnerabilities (vite, vitest, follow-redirects,
  source-map-js, brace-expansion); builds install exactly the locked versions (`npm ci`).

### Changed

- The *Email* menu item moved into the *Company* page (`/email` redirects there).
- Employers no longer request leave; they log time off as hours.
- Dates follow the chosen language.
- Passwords set by someone else (new accounts and resets) are temporary: the person must
  choose their own at their next login.
- Hour types look the same everywhere: a pill with a dot in the hour type's color.
- The menu folds behind a menu button on laptops and phones, and every page fits a phone
  screen; wide tables scroll inside their own box.
- Tables keep their rows and buttons aligned, and fields, dropdowns and checkboxes look the
  same everywhere, also in dark mode.
- Manage employees shows the email under the name, and Leave shows the comment under the
  period, so both tables fit on a laptop.
- The browser tab shows a clock icon in the brand color.
- Changing your password signs you out on your other devices. `POST /api/auth/change-password`
  now returns a new sign-in instead of an empty response.

### Fixed

- Running a report with any hours in it showed a blank page (since 0.3.0).
- Making an hour type the default while another one was the default failed with a server error.
- Hour type names that differ only in capitals (e.g. "work" and "Work") are now caught as
  duplicates.
- Texts that were always English (hour type form, project details, list/planboard toggle)
  are now translated, and English texts use sentence case throughout.
- CSV exports quote names with commas, quotes or line breaks correctly, and names can't run as
  spreadsheet formulas.
- Deactivating a person or company, or resetting their password, now signs them out right
  away instead of when their sign-in expires (up to 8 hours later).
- Docker health checks: containers no longer show as unhealthy.
- Editing an hour type no longer saves the create form's color.

## [0.5.0] - 2026-10-27

### Added

- **Multiple companies per installation.** Each company has its own people, hour types,
  projects and hours, and can't see another company's data.
- **Platform admins**, who create, deactivate and reactivate companies but can't see their
  data.

### Changed

- **Breaking:** existing databases aren't migrated and must be recreated
  (`docker compose down -v`). Rename `SEED_EMPLOYER_*` to `SEED_ADMIN_*` in `.env`; the
  bootstrap account is now a platform admin.

## [0.4.0] - 2026-10-27

### Added

- Optional location on time entries, with a *Use my location* button.
- Docker images are built and published for every version tag.

### Fixed

- Hour type names are checked for duplicates in every language, not only English.

## [0.3.0] - 2026-09-13

### Added

- Projects.
- Hour type names in several languages.

## [0.2.1] - 2026-09-11

### Added

- English, Dutch and French.
- The header uses the primary brand color.

## [0.2] - 2026-09-11

### Added

- Docker images on Docker Hub.

## [0.1] - 2026-09-11

### Added

- First release: logging hours, list and planboard views, reports per day, week and month,
  and the Employee, Supervisor and Employer roles.

[Unreleased]: https://github.com/detono/timetracker/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/detono/timetracker/compare/v0.5.0...v1.0.0
[0.5.0]: https://github.com/detono/timetracker/compare/v0.4.0...v0.5.0
[0.4.0]: https://github.com/detono/timetracker/compare/v0.3.0...v0.4.0
[0.3.0]: https://github.com/detono/timetracker/compare/v0.2.1...v0.3.0
[0.2.1]: https://github.com/detono/timetracker/compare/v0.2...v0.2.1
[0.2]: https://github.com/detono/timetracker/compare/v0.1...v0.2
[0.1]: https://github.com/detono/timetracker/releases/tag/v0.1
