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
- Added a homepage with navigation cards for the agenda, new appointment creation, and participant search
- Added a live appointment result counter powered by `HEAD /appointments`, visible even when no items are returned
- Added a cancellable appointment creation flow with dirty-form confirmation before leaving the page
- Added a participants search page stub as the first entry point for attendee lookup

#### API
- Added `DELETE /appointments/{id}/attendees/{id}` endpoint
- Added RabbitMQ integration and related configuration.
- Added publication of `AppointmentScheduled` event when a new appointment is scheduled : 
- Added publication of `AppointmentCreated` event when a new appointment is created, containing appointment ID, start/end dates (ISO 8601), location, attendees list, and creator ID (#329)
- Added healthchecks for PostgreSQL and RabbitMQ
- Migrated API documentation UI from Swagger UI to Scalar while keeping the OpenAPI JSON document available ([#571](https://github.com/candoumbe/agenda/issues/571))
- Updated appointments paginated headers contract for UI navigation: `total` now represents the total number of matching elements, `count` represents the number of elements in the current page, and redundant `totalCount` was removed
- Added multi-criteria filtering for appointments listing (`subject`, `location`, and `from`/`to` time range)
- Fixed appointments search query binding for ISO `OffsetDateTime` range filters so first-load requests return `200` instead of `400`
- Added `HEAD` support headers on appointments `GET` endpoints: `Link` remains the navigation header, and paginated collections emit `Link`, `total`, and `count` headers
- Added integration coverage for appointments `HEAD` contracts on `GET /appointments/{id}` and paginated `GET /appointments` responses
- Made appointments search case-insensitive at database level by switching `Subject` and `Location` to PostgreSQL `citext` columns ([#504](https://github.com/candoumbe/agenda/issues/504))

#### AppHost
- Aspire AppHost now provisions a Keycloak resource (`keycloak`) with an imported `agenda` realm including dev users `alice` and `admin`. ([#576](https://github.com/candoumbe/agenda/issues/576), [#323](https://github.com/candoumbe/agenda/issues/323))

#### Authentication
- API now validates Keycloak-issued JWTs (audience `agenda-api`, RS256 only) with `realm_access.roles` flattened into `ClaimTypes.Role` claims. ([#577](https://github.com/candoumbe/agenda/issues/577), [#323](https://github.com/candoumbe/agenda/issues/323))
- Default authorization policy now requires an authenticated user but OpenAPI/Scalar routes remain anonymous in non-Production environments only. ([#577](https://github.com/candoumbe/agenda/issues/577))
- Removed the symmetric `JwtOptions` configuration in favor of Keycloak OIDC discovery. ([#577](https://github.com/candoumbe/agenda/issues/577))

### 📝 Documentation
- Added ADR-001: Authentication provider selection (Keycloak) ([#323](https://github.com/candoumbe/agenda/issues/323))

### 🧹 Housekeeping
- Excluded `docs/**` from CI triggers on both `push` and `pull_request` events
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