# Plan: Keycloak Authentication

> Date: 2026-05-29
> Scope: [src/Agenda.AppHost](../../src/Agenda.AppHost/), [src/Agenda.API](../../src/Agenda.API/), [tests/Agenda.API.IntegrationTests](../../tests/Agenda.API.IntegrationTests/), [tests/Agenda.API.UnitTests](../../tests/Agenda.API.UnitTests/), [tests/Agenda.API.ArchitecturalTests](../../tests/Agenda.API.ArchitecturalTests/)
> Status: Approved
> Tracking issue: [#323](https://github.com/candoumbe/agenda/issues/323)
> Related ADR: [docs/ADRs/001-authentication-provider.md](../ADRs/001-authentication-provider.md)
> Branch: `feature/authentication-support`

## 1) Goal And Constraints

- [ ] All API endpoints require a valid JWT issued by Keycloak; anonymous access is restricted to a strict allow-list.
- [ ] Keycloak is provisioned through the existing .NET Aspire AppHost — no parallel docker-compose stack.
- [ ] FastEndpoints' built-in security model is the single source of authorization (per [FastEndpoints — Security](https://fast-endpoints.com/docs/security)).
- [ ] Vertical-slice architecture is preserved: cross-cutting auth wiring lives outside `Agenda.API.Features.*`.
- [ ] Repository conventions in [AGENTS.md](../../AGENTS.md) are respected (explicit types, `_field` naming, `using` outside namespace).
- [ ] Frontend integration (Phase 5 in the original plan) is out of scope for this work and tracked separately.

## 2) Decisions

- [ ] Realm `agenda`, two realm roles: `agenda-user` (default) and `agenda-admin`.
- [ ] Three OIDC clients in addition to the bearer-only API client:
  - [ ] `agenda-frontend` — public, Authorization Code + PKCE (S256), for the Angular SPA.
  - [ ] `agenda-mobile` — public, Authorization Code + PKCE (S256), custom-scheme redirect URI for the future mobile app.
  - [ ] `agenda-api` — bearer-only, audience target.
  - [ ] `agenda-service` — confidential, `client_credentials` grant, dedicated `agenda-service-account` role for service-to-service calls.
- [ ] Audience mappers on `agenda-frontend`, `agenda-mobile`, and `agenda-service` add `agenda-api` to the `aud` claim.
- [ ] `realm_access.roles` is flattened into individual `ClaimTypes.Role` claims via `IClaimsTransformation` so `User.IsInRole()` and FastEndpoints `Roles(...)` work.
- [ ] Existing [src/Agenda.API/JwtOptions.cs](../../src/Agenda.API/JwtOptions.cs) is deleted (symmetric `Key` is incompatible with RS256/discovery).
- [ ] Token lifetimes: access 5 min · refresh idle 30 min · refresh max 8 h.
- [ ] Validation parameters: `ValidAlgorithms = ["RS256"]`, `ClockSkew = 30s`, `MapInboundClaims = false`, `NameClaimType = "preferred_username"`.
- [ ] Anonymous allow-list: `/health`, `/alive`, plus `/openapi/*` and `/scalar/*` **only when `app.Environment.IsProduction() == false`** — Production requires authentication for documentation routes.
- [ ] Test strategy: in-memory `TokenFactory` (RSA + JWKS) for the bulk of integration tests, plus a single `KeycloakSmokeTests` class against a real Keycloak Testcontainer tagged `[Trait("Category","Smoke")]`.

## 3) Phased Execution

### Phase 1 — AppHost & Realm (`squad:bishop`)

- [ ] Add `Aspire.Hosting.Keycloak` to [Directory.Packages.props](../../Directory.Packages.props) (aligned with the existing Aspire `13.3.5` band) and reference it in [src/Agenda.AppHost/Agenda.AppHost.csproj](../../src/Agenda.AppHost/Agenda.AppHost.csproj).
- [ ] Add Aspire parameters `keycloak-admin-user` and `keycloak-admin-password` (`secret: true`).
- [ ] Wire `builder.AddKeycloak("keycloak", admin, password)` chained with `WithImageTag("26.x")`, `WithDataVolume("keycloak-data")` (skipped under integration tests), and `WithRealmImport("./keycloak/agenda-realm.json")`.
- [ ] Chain `.WithReference(keycloak).WaitFor(keycloak)` on the `api` resource (and on `frontend` once it lands).
- [ ] Create [src/Agenda.AppHost/keycloak/](../../src/Agenda.AppHost/) folder containing:
  - [ ] `agenda-realm.json` — realm `agenda`, four clients, two roles, audience and roles-flatten mappers, seeded dev users `alice`/`password` (role `agenda-user`) and `admin`/`password` (roles `agenda-user` + `agenda-admin`), Direct Access Grant enabled on `agenda-frontend` for dev only.
  - [ ] `README.md` documenting the export/import workflow (`kc.sh export --realm agenda`).
- [ ] Mark realm JSON as `Content` with `CopyToOutputDirectory=PreserveNewest`.

### Phase 2 — API JWT Validation (`squad:bishop`)

- [ ] Add `Aspire.Keycloak.Authentication` and `Microsoft.AspNetCore.Authentication.JwtBearer` (explicit pin) to [Directory.Packages.props](../../Directory.Packages.props) and reference them in [src/Agenda.API/Agenda.API.csproj](../../src/Agenda.API/Agenda.API.csproj).
- [ ] Delete [src/Agenda.API/JwtOptions.cs](../../src/Agenda.API/JwtOptions.cs) and any binding it had in [src/Agenda.API/ServiceCollectionExtensions.cs](../../src/Agenda.API/ServiceCollectionExtensions.cs).
- [ ] Add a new `AddCustomAuthentication(IConfiguration, IHostEnvironment)` extension in [src/Agenda.API/ServiceCollectionExtensions.cs](../../src/Agenda.API/ServiceCollectionExtensions.cs) that calls `AddAuthentication(...).AddKeycloakJwtBearer("keycloak", "agenda", configureOptions)` with audience `agenda-api`, environment-driven `RequireHttpsMetadata`, `MapInboundClaims = false`, `ValidAlgorithms = ["RS256"]`, `ClockSkew = 30s`.
- [ ] Add `services.AddAuthorization()` with a default `RequireAuthenticatedUser()` policy.
- [ ] Implement an `IClaimsTransformation` that flattens `realm_access.roles` into `Claim(ClaimTypes.Role, ...)`.
- [ ] Update [src/Agenda.API/Program.cs](../../src/Agenda.API/Program.cs) pipeline order: `UseAuthentication()` → `UseAuthorization()` → `UseFastEndpoints(c => c.Security.RoleClaimType = ClaimTypes.Role)` → `UseOpenApi(...)` → `MapScalarApiReference(...)` → `MapDefaultEndpoints()`.
- [ ] Gate Scalar/OpenAPI endpoints with explicit `AllowAnonymous` only when not in Production.
- [ ] Extend [src/Agenda.API/CurrentRequestMetadataInfoProvider.cs](../../src/Agenda.API/CurrentRequestMetadataInfoProvider.cs) with `GetCurrentUserId()` (parsed from `sub`) and `GetCurrentUserName()` (from `preferred_username`).
- [ ] Update [src/Agenda.API/appsettings.json](../../src/Agenda.API/appsettings.json), [appsettings.Development.json](../../src/Agenda.API/appsettings.Development.json), and [appsettings.IntegrationTest.json](../../src/Agenda.API/appsettings.IntegrationTest.json) with the new `Authentication:Keycloak` section.

### Phase 3 — Endpoint Protection (`squad:bishop`)

- [ ] Confirm every endpoint under [src/Agenda.API/Features/Appointments/v1/](../../src/Agenda.API/Features/Appointments/v1/) inherits the default authenticated policy (no per-endpoint code change required).
- [ ] Tighten `Features/Appointments/v1/Delete` with `Roles("agenda-admin")` as the v1 example of the admin role.
- [ ] Confirm `/health` and `/alive` (Aspire ServiceDefaults) remain anonymous as non-FastEndpoints routes.

### Phase 4 — Tests (`squad:hicks`)

- [ ] Add architectural rules in [tests/Agenda.API.ArchitecturalTests](../../tests/Agenda.API.ArchitecturalTests/):
  - [ ] Rule A — every `IEndpoint` is either authenticated or appears in a named anonymous allow-list.
  - [ ] Rule B — `*JwtBearerOptions*`, `*KeycloakOptions*`, `*AuthenticationHandler*` types live outside `Agenda.API.Features.*`.
  - [ ] Rule C — types under `Agenda.API.Features.*` do not reference the Keycloak SDK directly.
- [ ] Add `TokenFactoryShould` and helpers in [tests/Agenda.UnitTests.Helpers](../../tests/Agenda.UnitTests.Helpers/):
  - [ ] `TokenFactory` exposing presets `ValidFor(sub, roles)`, `Expired()`, `WrongAudience()`, `WrongIssuer()`, `UnsignedOrTampered()`, plus a JWKS publisher for the integration fixture.
- [ ] Unit tests in [tests/Agenda.API.UnitTests](../../tests/Agenda.API.UnitTests/):
  - [ ] `CurrentRequestMetadataInfoProviderShould` for `sub` / `preferred_username` / roles parsing.
  - [ ] Claims transformation flatten test for `realm_access.roles`.
- [ ] Integration tests in [tests/Agenda.API.IntegrationTests](../../tests/Agenda.API.IntegrationTests/):
  - [ ] Extend `AgendaApplicationFixture` with an authenticated `ApiClient` (default token = `agenda-user`) and a new `AnonymousApiClient`.
  - [ ] Add per-endpoint negative tests (401 missing token, 401 expired, 401 wrong audience, 401 wrong issuer, 401 wrong signature, 403 missing `agenda-admin` on `Delete`).
  - [ ] Add `KeycloakSmokeTests` `[Trait("Category","Smoke")]` validating discovery + token mint against the real Keycloak Testcontainer through Aspire.
- [ ] Validation: `./build.sh architectural-tests`, `./build.sh unit-tests`, `./build.sh Tests` all green.

### Phase 5 — Documentation (`squad:lambert`)

- [ ] Create [docs/development/authentication.md](../development/authentication.md) covering: local startup, seeded users, fetching a token via `curl` (Direct Access Grant), calling the API, fetching a service token (`client_credentials`), inspecting tokens, troubleshooting (401 audience/issuer/clock).
- [ ] Update [README.md](../../README.md) with an "Authentication" section linking to [docs/development/authentication.md](../development/authentication.md) and [docs/ADRs/001-authentication-provider.md](../ADRs/001-authentication-provider.md).
- [ ] Update [CHANGELOG.md](../../CHANGELOG.md) with an `### Added` entry referencing #323.
- [ ] Update [docs/feature/aspire-integration.md](../feature/aspire-integration.md) with a Keycloak resource section.

## 4) Risks And Mitigations

- [ ] Risk: realm export drifts from runtime configuration.
  - Mitigation: keep a single source of truth (`agenda-realm.json`), document export workflow, validate via the smoke test.
- [ ] Risk: Keycloak Testcontainer cold-start (~10–20 s) impacts CI runtime.
  - Mitigation: keep the smoke class minimal (one or two scenarios), reuse a single Keycloak instance per session via the existing assembly fixture pattern.
- [ ] Risk: existing integration tests start failing en masse once the middleware is wired.
  - Mitigation: make `AgendaApplicationFixture.ApiClient` authenticated by default (token issued by `TokenFactory`); add `AnonymousApiClient` for explicit negative cases.
- [ ] Risk: claim mapping mismatches between the realm export and the .NET configuration.
  - Mitigation: assert claim shape in unit tests for the claims transformation; validate end-to-end via the Keycloak smoke test.

## 5) Validation Checklist

- [ ] `./build.sh architectural-tests` green (including the three new auth rules).
- [ ] `./build.sh unit-tests` green.
- [ ] `./build.sh Tests` green (full pipeline including integration + smoke).
- [ ] Manual smoke: `dotnet run --project src/Agenda.AppHost` → Keycloak admin console reachable → token obtained for `alice` → `GET /api/v1/appointments` returns 200 with the bearer token and 401 without.
- [ ] Manual smoke: `client_credentials` token obtained for `agenda-service` → protected endpoint returns 200.
- [ ] [docs/development/authentication.md](../development/authentication.md) walkthrough verified end-to-end on a clean checkout.
