# Changelog

All notable changes to TimeTracker are listed here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and versions follow
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

Database migrations run automatically when the API starts, unless a release says otherwise.

## [Unreleased]

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

### Changed

- The *Email* menu item moved into the *Company* page (`/email` redirects there).
- Employers no longer request leave; they log time off as hours.
- Dates follow the chosen language.

### Fixed

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

[Unreleased]: https://github.com/detono/timetracker/compare/v0.5.0...HEAD
[0.5.0]: https://github.com/detono/timetracker/compare/v0.4.0...v0.5.0
[0.4.0]: https://github.com/detono/timetracker/compare/v0.3.0...v0.4.0
[0.3.0]: https://github.com/detono/timetracker/compare/v0.2.1...v0.3.0
[0.2.1]: https://github.com/detono/timetracker/compare/v0.2...v0.2.1
[0.2]: https://github.com/detono/timetracker/compare/v0.1...v0.2
[0.1]: https://github.com/detono/timetracker/releases/tag/v0.1
