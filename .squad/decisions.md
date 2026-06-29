# Squad Decisions

## Active Decisions

### 2026-06-29T12:00:00Z: Issue #623 Phase 1 — Endpoint security inventory baseline
**By:** Vasquez
**What:** Full audit of FastEndpoints endpoints identified 4 endpoints with incorrect `AllowAnonymous()` (Create, Search, GetById, Patch appointments) and 1 partial gap (`RemoveAttendee` no role restriction). Documentation and `/scalar` surfaces confirmed intentionally public.
**Why:** Establish the security baseline before access control hardening in subsequent phases.
**Impact:** Inventory drives Phase 2 fix and Phase 4 test coverage. Full findings in `docs/plans/issue-623-endpoint-inventory.md`.

### 2026-06-29T12:00:00Z: Issue #623 Phase 2 — Remove AllowAnonymous from appointment CRUD endpoints
**By:** Bishop
**What:** Removed `AllowAnonymous()` from `CreateAppointmentEndpoint`, `SearchAppointmentsEndpoint`, `GetAppointmentByIdEndpoint`, and `PatchAppointmentByIdEndpoint`. FallbackPolicy `RequireAuthenticatedUser` now applies. `/scalar` and `/openapi` remain intentionally public.
**Why:** AllowAnonymous on CRUD endpoints silently bypassed the FallbackPolicy, exposing unauthenticated mutations and reads.
**Impact:** All appointment CRUD endpoints now require an authenticated user. No explicit `[Authorize]` needed — FallbackPolicy is sufficient.

### 2026-06-29T12:00:00Z: Issue #623 Phase 4 — Security integration tests for appointment endpoints
**By:** Hicks
**What:** Added `tests/Agenda.API.IntegrationTests/Authentication/AppointmentsEndpointsAuthorizationShould.cs`. Each of the 4 endpoints tested for: 401 (no token), 401 (expired / wrong audience / wrong issuer / tampered tokens), and non-401/403 (valid Keycloak token). Uses `AnonymousApiClient`, `TokenFactory`, and `IssueAccessTokenAsync`.
**Why:** Phase 4 of issue #623 requires test coverage validating 401/403 behavior after access control hardening.
**Impact:** Regression baseline for appointment endpoint authentication is established and will catch future AllowAnonymous regressions.

### 2026-06-29T12:00:00Z: Issue #623 — Centralize auth in AgendaApplicationTestingBuilder to fix integration test regression
**By:** Bishop
**What:** After AllowAnonymous removal, existing integration tests failed with 401. Fix centralized in `AgendaApplicationTestingBuilder.StartAsync`: (1) readiness probe changed from `GET /appointments?…` to `GET /health`; (2) real Keycloak token obtained via `IssueAccessTokenAsync("alice", "password")` and injected into `ApiClient.DefaultRequestHeaders.Authorization`. No individual test files modified.
**Why:** Touching every test individually would be high-noise and brittle. A single-point fix in the fixture builder is safer, consistent, and preserves `AnonymousApiClient` behavior for Hicks' negative tests.
**Impact:** All existing functional integration tests pass without modification. Auth tests continue to use `AnonymousApiClient` correctly.

### 2026-06-06T18:42:11Z: Frontend auth navigation contract for Keycloak flow
**By:** Dallas
**What:** Standardize frontend authentication navigation behavior:
- Protected routes redirect unauthenticated users to `/login` with `redirectTo`.
- Callback handling only accepts safe relative redirect targets and falls back to `/`.
- Logout from app shell always navigates to `/login`.
**Why:** Remove mixed legacy/OIDC assumptions, keep login/logout behavior predictable, and prevent open redirect regressions.
**Impact:** Route and shell auth behavior is deterministic and directly testable.

### 2026-06-06T18:42:11Z: Frontend auth regression baseline and validation gate
**By:** Hicks
**What:** Maintain regression coverage for auth routing and topbar logout behavior with focused route/topbar tests and full-suite verification.
**Why:** Keep high-risk auth entry/exit paths stable as Keycloak integration evolves.
**Impact:** Validation baseline includes `npm run test -- --watch false` and `npm run build` in `src/Agenda.Frontend`.

### 2026-06-01T08:43:38Z: Aspire startup triage must validate current advertised endpoints first
**By:** Scribe (requested by Cyrille NDOUMBE)
**What:** During Aspire startup incident triage, validate reachability against the current run's advertised URLs and process exit status before classifying `connection refused` as an application regression.
**Why:** Dynamic endpoint rotation and mixed startup outcomes can produce false outages when stale URLs are reused.
**Impact:** Faster, lower-noise local diagnosis during backend/frontend startup investigations.

### 2026-05-29T13:32:53Z: User directive - Link markdown docs using markdown links
**By:** vscode (via Copilot)
**What:** In markdown files, references to other markdown files must use markdown links.
**Why:** User request - captured for team memory.

### 2026-05-29T00:00:00Z: Keycloak Phase 1 AppHost and realm baseline
**By:** Bishop
**What:** Establish Keycloak realm/apphost foundation for Agenda authentication:
- Realm `agenda` with default `agenda-user` role and explicit `agenda-admin` and service-account roles.
- Client baseline for frontend/mobile/API/service with audience + realm-role mappers.
- AppHost references for API and frontend waiting on Keycloak readiness.
**Why:** Provide the canonical identity baseline before API enforcement and endpoint-level authorization.

### 2026-05-29T00:00:00Z: Keycloak Phase 2 API JWT validation defaults
**By:** Bishop
**What:** Enforce API JWT validation with Keycloak realm/audience configuration, claims transformation for realm roles, and authenticated fallback policy.
**Why:** Make authentication default-on while keeping anonymous exposure explicit and controlled.
**Impact:** API authentication is centralized, role extraction is deterministic, and anonymous surfaces are intentionally limited.

### 2026-05-29T00:00:00Z: Keycloak Phase 3 endpoint role protection scope
**By:** Bishop
**What:** Protect `DELETE /appointments/{id}` with `agenda-admin` role while leaving other endpoint `AllowAnonymous()` calls untouched pending follow-up hardening.
**Why:** Deliver incremental endpoint authorization without broad-scope refactoring in the same phase.
**Impact:** Delete operation is role-restricted now; remaining anonymous annotations are tracked as follow-up risk.

### 2026-05-29T07:08:11Z: User directive - Present results as dashboard/table
**By:** Cyrille NDOUMBE (via Copilot)
**What:** Going forward, present results in a dashboard/table format.
**Why:** User request - captured for team memory.

### 2026-05-29T07:26:14Z: Backend documentation UI surface should use Scalar
**By:** Bishop
**What:** Replace runtime Swagger UI exposure with Scalar while keeping FastEndpoints/OpenAPI document generation and JSON exposure.
**Why:** Complete the Swagger UI to Scalar migration with minimal backend churn and preserve tooling compatibility.
**Impact:** Local documentation entrypoint is Scalar, Swagger UI route is removed, and OpenAPI JSON remains available.

### 2026-05-29T07:26:14Z: Documentation endpoint regression coverage must validate UI and OpenAPI contract
**By:** Hicks
**What:** Integration tests for documentation routing should validate three behaviors together: Scalar UI is reachable, Swagger UI is not exposed, and v1 OpenAPI JSON is available and valid.
**Why:** UI-only checks can miss regressions where documentation rendering depends on missing or invalid OpenAPI output.
**Impact:** Stronger release confidence for API consumers and tooling that rely on OpenAPI.

### 2026-06-28T23:16:38Z: User directive — All documents must be in English
**By:** Cyrille NDOUMBE (via Copilot)
**What:** All documents in this repository must be written in English. This supersedes the earlier "internal drafts may be French" allowance (2026-06-28T23:01:35Z).
**Why:** Public repository — English is required for all content including internal drafts.

### 2026-05-26T21:08:36Z: User directive — English-only user-facing docs
**By:** Cyrille NDOUMBE (via Copilot)
**What:** Keep every user-facing documentation in English for this public repository.
**Why:** User request — captured for team memory.

### 2026-04-24T00:00:00Z: User directive - Follow CONTRIBUTING.md
**By:** vscode (via Copilot)
**What:** All squad members must follow CONTRIBUTING.md at all times. Key rules:
- Branch workflow: topic branches from `develop` (feature/*, coldfix/*, chore/*, etc.) - never from `main` except `hotfix/*`
- Commit messages: Conventional Commits format (`<type>[scope]: <description>`) in imperative mood with atomic commits
- Code style: avoid `var` except anonymous types; prefer explicit types; prefer single entry/single exit when reasonable
- Tests: unit/integration tests required for every new feature or behavior change
- PRs: target `develop`, include a clear description and tests
**Why:** User directive governing all squad work in this repository.

### 2026-04-24T12:00:00Z: Scheduling UI contract alignment
**By:** Dallas
**What:** For the appointment scheduling flow, frontend payloads must align to current API expectations:
- Use `POST /api/appointments` (proxy to `/appointments`)
- Use `phoneNumber` for attendee phone data
- Use ISO datetime strings for start/end values
**Why:** Prevent payload mismatches and keep scheduling creation compatible with API validators.
**Impact:** New scheduling screen can create appointments end-to-end; future list/search UI should consume paginated responses.

### 2026-04-24T12:00:00Z: Null attendees normalization on create appointment
**By:** Bishop
**What:** Backend create appointment flow accepts `attendees: null` and normalizes to an empty list in endpoint handling.
**Why:** Reduce unnecessary frontend/backend coupling around collection initialization and avoid non-essential validation failures for legitimate UI flows.
**Scope:** `NewAppointmentInfoValidator`, create endpoint normalization, and related create flow tests.
**Impact:** No route or namespace changes; vertical-slice architecture remains unchanged.

### 2026-04-26T12:00:00Z: Appointments listing pagination metadata contract
**By:** Bishop, Dallas, Hicks
**What:** Standardize listing/search pagination semantics and client behavior:
- `page`: page served by backend (source of truth)
- `total`: total pages
- `totalCount`: total matching items
- `pageSize`: requested page size
- `count`: items returned on current page
- Pagination links (`first`, `last`, `previous`, `next`) must be derived from total pages, not current-page item count.
**Why:** Remove ambiguity between item-count and page-count semantics and prevent frontend page drift.
**Impact:** Deterministic pagination across API and UI; clearer contract for future consumers.

### 2026-04-26T12:00:00Z: Link-first frontend pagination and multi-criteria listing search
**By:** Dallas
**What:** For appointments list UX:
- Use API links (`links.previous`, `links.next`, `links.last`) as primary navigation bounds.
- Keep displayed page synchronized from `response.page`.
- Derive displayed total pages from `links.last` when present, fallback to `response.total`.
- Send multi-criteria filters `subject`, `location`, `from`, `to` to listing endpoint.
**Why:** Keep UI navigation robust when backend clamps/adjusts page values and improve search usefulness.
**Impact:** Stable pagination UX, better search coverage, and improved frontend test confidence.

### 2026-04-26T09:54:11Z: User directive - Agent history language
**By:** Cyrille NDOUMBE (via Copilot)
**What:** All agent history files must always be written in English.
**Why:** Ensure consistency and readability across all team history artifacts.

### 2026-04-28T16:08:01Z: User directive - Reinforce GitFlow and CONTRIBUTING
**By:** Cyrille NDOUMBE (via Copilot)
**What:** Team must strictly follow GitFlow and read `CONTRIBUTING.md` before any code modification.
**Why:** Reinforce workflow and contribution discipline as non-optional preconditions for code changes.

### 2026-04-26T18:00:00Z: Unit-tests pipeline compatibility with Microsoft.Testing.Platform
**By:** Hicks (via Scribe)
**What:** `./build.sh unit-tests` is blocked by two independent issues:
- `UnitTests` depends on `Format`, so formatting violations can abort before tests run.
- Nuke's `DotNetTest` invocation passes options incompatible with Microsoft.Testing.Platform (`--logger`, `--results-directory`).
**Decision:** Update `UnitTests` in `build/Build.cs` to use an MTP-compatible test invocation, and decouple `Format` from `UnitTests` or provide a documented skip path.
**Why:** Restore reliable and actionable unit-test execution in local and CI workflows.

### 2026-05-23T21:10:00Z: Issue #541 interval discovery and jump behavior
**By:** Dallas
**What:** For appointment listing issue #541, implement frontend behavior in two explicit steps:
- Primary query uses the active filter window (default [today, today+15 days] on first load).
- If interval result is empty, run a secondary query with `from=<selected to>`, `page=1`, `pageSize=1` to discover the first incoming appointment after the window and enable a jump action.

Jump action updates `from` to the discovered appointment start date and `to` to `from + 15 days`, then reloads the list.
**Why:** Satisfy the UX requirement without backend contract changes while keeping the implementation minimal and testable.

### 2026-05-23T21:10:00Z: Frontend tests for issue #541 must assert two-phase empty-range behavior
**By:** Hicks
**What:** When the list interval query returns empty results with a bounded date range, frontend behavior should be validated in two steps: first query for the selected interval, then fallback query for the first incoming appointment (`pageSize: 1`, `from = selected to`). Tests should assert both the main interval request and the fallback request where applicable.
**Why:** Prevent brittle assertions and ensure acceptance coverage for default 15-day interval, empty-state CTA, and jump-to-first-incoming flow.

### 2026-05-26T20:22:42Z: HEAD parity response-header strategy for GET endpoints
**By:** Bishop (via Scribe)
**What:** Standardize GET and HEAD response-header parity through a shared interceptor pattern that emits navigation metadata consistently (`Link`, and pagination headers where applicable).
**Why:** Keep behavior aligned across current and future GET endpoints while avoiding duplicated header logic per feature slice.
**Scope:** Appointment GET endpoints and related response metadata.

### 2026-05-19T00:00:00Z: Testing standards directive - prefer AwesomeAssertions
**By:** Cyrille NDOUMBE (via Copilot)
**What:** Squad always prefers using AwesomeAssertions over raw xUnit Assert for this project.
**Why:** User directive for consistent test style across the Agenda project.
**Applies to:** All test code in the Agenda project.

### 2026-05-24T07:29:04Z: User directive - Separate commit for Squad files
**By:** Cyrille NDOUMBE (via Copilot)
**What:** Always create a dedicated, separate commit for all Squad files.
**Why:** User request captured for team workflow consistency.

### 2026-05-24T07:32:06Z: User directive - PR title and description language
**By:** Cyrille NDOUMBE (via Copilot)
**What:** For this public repository, pull request titles and descriptions must be written in English.
**Why:** User request captured for collaboration and external readability consistency.

### 2026-05-27T00:00:00Z: Replace RetryFact with Fact in test suite
**By:** Hicks
**What:** Replace remaining `RetryFact` usage in integration tests with plain `Fact` and remove the corresponding `xRetry.v3` usings from touched files.
**Why:** Keep test execution deterministic and avoid hiding flakiness behind automatic retries now that the affected integration tests run reliably without retry wrappers.

### 2026-05-28T01:12:06Z: User directive - Always use single entry/single exit when coding
**By:** Cyrille NDOUMBE (via Copilot)
**What:** When the team writes code, always use the single entry / single exit approach.
**Why:** User request - captured for team memory.

### 2026-05-28T20:08:30Z: Frontend pagination should prioritize HATEOAS headers
**By:** Dallas
**What:** Frontend pagination/navigation metadata should be resolved from HTTP response headers first (`Link`, `total`, `count`) and only fallback to legacy body pagination fields when headers are absent.
**Why:** The backend now emits canonical HATEOAS/count metadata in headers; header-first parsing avoids stale or partial body metadata.

### 2026-05-28T20:09:00Z: Frontend pagination link parser must support parameter case variants
**By:** Bishop
**What:** Frontend extraction of pagination query values from HATEOAS links must handle both lowercase and PascalCase query parameter names (for example `page` and `Page`) when reading link URLs.
**Why:** `URLSearchParams` is case-sensitive and backend-generated links may use PascalCase, which can otherwise force incorrect page fallback behavior.

### 2026-05-28: Homepage route and HEAD counter pattern
**By:** Dallas
**What:**
- Route `''` now loads `HomePageComponent` directly (no more `redirectTo: 'appointments'`).
- `ApiService.countAppointments()` uses HTTP HEAD to retrieve the `total` header independently of GET — both calls fire in parallel in `AppointmentsListPageComponent.loadAppointments()`.
- Route `/attendees` added and served by `AttendeesSearchPageComponent` (stub).
**Why:** User request — three distinct frontend features shipped atomically.
**Impact:** Entry URL lands on the new homepage; appointments list shows a persistent result count badge; attendees route is registered for future implementation.

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction
