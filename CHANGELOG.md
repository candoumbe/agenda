# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/)
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]
### 🚀 New features

#### Frontend

- Added UI for scheduling an appointment
- Added appointments list page with card-based layout, date grouping, search by subject, and pagination (#502)
- Added automatic redirect to appointments list after creating a new appointment (#502)
- Added default 15-day appointments window with contextual empty-state guidance and jump-to-first-incoming action ([#541](https://github.com/candoumbe/agenda/issues/541))

#### API
- Added `DELETE /appointments/{id}/attendees/{id}` endpoint
- Added RabbitMQ integration and related configuration.
- Added publication of `AppointmentScheduled` event when a new appointment is scheduled : 
- Added publication of `AppointmentCreated` event when a new appointment is created, containing appointment ID, start/end dates (ISO 8601), location, attendees list, and creator ID (#329)
- Added healthchecks for PostgreSQL and RabbitMQ
- Updated appointments paginated headers contract for UI navigation: `total` now represents the total number of matching elements, `count` represents the number of elements in the current page, and redundant `totalCount` was removed
- Added multi-criteria filtering for appointments listing (`subject`, `location`, and `from`/`to` time range)
- Fixed appointments search query binding for ISO `OffsetDateTime` range filters so first-load requests return `200` instead of `400`
- Added `HEAD` support headers on appointments `GET` endpoints: `Link` remains the navigation header, and paginated collections emit `Link`, `total`, and `count` headers
- Added integration coverage for appointments `HEAD` contracts on `GET /appointments/{id}` and paginated `GET /appointments` responses
- Made appointments search case-insensitive at database level by switching `Subject` and `Location` to PostgreSQL `citext` columns ([#504](https://github.com/candoumbe/agenda/issues/504))

### 🧹 Housekeeping
- Added task issue template
- Updated bug issue template
- Added Codecov configuration file
- Fixed Aspire AppHost resource startup blocking by removing custom TCP health checks wired to PostgreSQL and RabbitMQ resources
- Fixed devcontainer .NET SDK provisioning to install `10.0.300` by default (with `10.0.203` and `10.0.201` as additional SDKs) to match project requirements and unblock Aspire startup
- Fixed integration test startup hangs by restoring the AppHost/fixture startup flow used on `develop` for integration mode
- Stabilized appointment creation integration coverage by retrying transient `5xx` responses during startup races
- Raised Angular `anyComponentStyle` warning budget to `5kB` to align build checks with current UI styles
- Updated `xRetry` to `1.0.0-rc2`
- Updated `xunit.v3` to `3.2.0`
- Updated `Paramore.Brighter.*` packages to `10.0.4`
- Bumped TFM to `net10.0`
- Added dotnet devcontainer feature to manage .NET versions

## [0.1.0] / 2025-10-03
### 🚀 New features
- Initial release of the Agenda module

[Unreleased]: https://github.com/candoumbe/agenda/compare/0.1.0...HEAD
[0.1.0]: https://github.com/candoumbe/agenda/tree/0.1.0