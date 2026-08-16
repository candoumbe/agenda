# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/)
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.2.0] / 2026-08-16
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
- Frontend OIDC settings are now loaded from a generated `runtime-auth.js` file at startup so the Angular app can pick up the auth authority, client ID, and scope from runtime environment variables without a rebuild

#### API
- Added `DELETE /appointments/{id}/attendees/{id}` endpoint
- Added RabbitMQ integration and related configuration.
- Added publication of `AppointmentScheduled` event when a new appointment is scheduled : 
- Added publication of `AppointmentCreated` event when a new appointment is created, containing appointment ID, start/end dates (ISO 8601), location, attendees list, and creator ID (#329)
- Updated appointment creation flow to persist and publish the authenticated creator username (`preferred_username`) instead of the static `system` value
- Added auditable appointment API resource fields to expose creator metadata on create responses
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
- Aspire AppHost now injects the frontend OIDC runtime settings (`AGENDA_AUTH_AUTHORITY`, `AGENDA_AUTH_CLIENT_ID`, `AGENDA_AUTH_SCOPE`) from the provisioned Keycloak realm so local frontend runs target the same auth server without manual configuration

#### Authentication
- API now validates Keycloak-issued JWTs (audience `agenda-api`, RS256 only) with `realm_access.roles` flattened into `ClaimTypes.Role` claims. ([#577](https://github.com/candoumbe/agenda/issues/577), [#323](https://github.com/candoumbe/agenda/issues/323))
- Default authorization policy now requires an authenticated user: OpenAPI/Scalar routes remain anonymous in every environment, including `Production`. ([#577](https://github.com/candoumbe/agenda/issues/577))
- Removed the symmetric `JwtOptions` configuration in favor of Keycloak OIDC discovery. ([#577](https://github.com/candoumbe/agenda/issues/577))
- All API endpoints now require authentication by default: `DELETE /appointments/{id}` additionally requires the `agenda-admin` realm role. ([#578](https://github.com/candoumbe/agenda/issues/578), [#323](https://github.com/candoumbe/agenda/issues/323))

### 🧪 Tests
- Migrated integration tests to the shared assembly fixture lifecycle to unify AppHost startup/teardown behavior across classes
- Stabilized targeted integration runs by hardening fixture readiness checks and timeout handling for local/devcontainer execution
- Updated Search `HEAD`/`GET` contract integration tests to use authenticated requests with explicit Bearer token handling
- Validated the full integration test suite locally with `33/33` passing tests
- Known limitation: `./build.sh integration-tests` is currently blocked in pipeline bootstrap because of a duplicate `github-pr-owner-number` key
- Added architectural rules enforcing endpoint authentication, authentication-type placement outside `Agenda.API.Features`, and Keycloak SDK isolation. ([#579](https://github.com/candoumbe/agenda/issues/579), [#323](https://github.com/candoumbe/agenda/issues/323))
- Added `TokenFactory` test helper producing valid, expired, wrong-audience, wrong-issuer, and tampered RS256 JWTs (with a public JWKS) for unit and integration tests. ([#579](https://github.com/candoumbe/agenda/issues/579))
- Added unit tests for current-user metadata extraction (`sub`, `preferred_username`) and `realm_access.roles` claim flattening. ([#579](https://github.com/candoumbe/agenda/issues/579))
- Added integration tests covering `401`/`403`/success paths via an authenticated fixture client and a new `AnonymousApiClient`. ([#579](https://github.com/candoumbe/agenda/issues/579))
- Added a Keycloak smoke test (`[Trait("Category","Smoke")]`) validating OIDC discovery and Direct Access Grant against the real Aspire-hosted Keycloak. ([#579](https://github.com/candoumbe/agenda/issues/579))
- Health endpoints (`/health`, `/alive`) are now anonymous so probes succeed under the JWT fallback policy.
- AppHost now trusts the ASP.NET Core developer certificate for the Keycloak resource so the API can reach the management endpoint over HTTPS in run mode.
- API documentation endpoints (`/openapi/{document}.json`, `/scalar`) are now anonymous in every environment so the JWT fallback policy never gates documentation access.
- Default authorization policy now requires an authenticated user but OpenAPI/Scalar routes remain anonymous in non-Production environments only. ([#577](https://github.com/candoumbe/agenda/issues/577))
- Removed the symmetric `JwtOptions` configuration in favor of Keycloak OIDC discovery. ([#577](https://github.com/candoumbe/agenda/issues/577))
- All API endpoints now require authentication by default. `DELETE /appointments/{id}` additionally requires the `agenda-admin` realm role. ([#578](https://github.com/candoumbe/agenda/issues/578), [#323](https://github.com/candoumbe/agenda/issues/323))
- Added unit coverage for `Username` value object validation and conversion
- Updated appointment creation unit tests to assert creator propagation from request metadata to emitted events and API response

### 📝 Documentation
- Added [docs/development/authentication.md](docs/development/authentication.md) describing the local Keycloak setup, seeded dev users, token-fetch recipes (Direct Access Grant, `client_credentials`), token inspection, and troubleshooting. ([#580](https://github.com/candoumbe/agenda/issues/580), [#323](https://github.com/candoumbe/agenda/issues/323))
- Linked the new authentication guide and ADR-001 from the [README](README.md). ([#580](https://github.com/candoumbe/agenda/issues/580))
- Documented the Keycloak resource and updated startup ordering in [docs/feature/aspire-integration.md](docs/feature/aspire-integration.md). ([#580](https://github.com/candoumbe/agenda/issues/580))
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
- Updated `Paramore.Brighter.*` packages to `10.6.0`
- Bumped TFM to `net10.0`
- Added dotnet devcontainer feature to manage .NET versions
- Updated AppHost launch settings to bind Aspire local service endpoints to `127.0.0.1` instead of `localhost`
- Updated `Candoumbe.Pipelines` package to `3.0.1`
- Enabled NuGet central transitive pinning and pinned `SQLitePCLRaw.lib.e_sqlite3` to a non-vulnerable version to address NU1903
- Removed the leftover commented-out NSwag configuration block from the API startup

### 🐛 Bug fixes

#### API
- Fixed API logging being silently disabled in containers by configuring Serilog from `appsettings.json` instead of binding the uninitialised static `Log.Logger`
- Fixed every API route returning `500` outside `Development` by resolving the Keycloak authority from Aspire service discovery instead of leaving the `https+http://` composite scheme as the JWT bearer metadata address
- Fixed the Scalar API reference rendering a blank page when reached with a trailing slash (`/scalar/v1/`) by redirecting its relative assets back to the canonical `/scalar/{asset}` path
- Fixed the Scalar API reference being unreachable outside `Development` by mapping it on every environment, including `Production`

#### AppHost
- Fixed containerised runs silently defaulting to the `Production` environment by propagating `ASPNETCORE_ENVIRONMENT` to the agenda API resource

## [0.1.0] / 2025-10-03
### 🚀 New features
- Initial release of the Agenda module

[Unreleased]: https://github.com/candoumbe/agenda/compare/0.2.0...HEAD
[0.2.0]: https://github.com/candoumbe/agenda/compare/0.1.0...0.2.0
[0.1.0]: https://github.com/candoumbe/agenda/tree/0.1.0
