# Squad Decisions

## Active Decisions

### 2026-07-12T00:00:00Z: Search HEAD contract tests authenticate request probes
**By:** Bishop
**What:** Updated `SearchAppointmentHeadContractShould` to request a real Keycloak token (`alice`) and send it in the `Authorization: Bearer` header for GET/HEAD contract assertions.
**Why:** The targeted Search HEAD contract tests were returning 401 Unauthorized when run anonymously. Explicit authentication aligns the class with current auth behavior while keeping test scope focused on response-header contract parity.

### 2026-07-12T00:00:00Z: Integration test environment stabilization for targeted runs
**By:** Hicks
**What:** Stabilized the integration-test assembly fixture startup path by replacing business-route readiness probing with `/health`, and added an environment override (`AGENDA_INTEGRATION_TESTS_STARTSTOP_TIMEOUT_SECONDS`) for startup timeout tuning in local/devcontainer scenarios.
**Why:** Targeted runs were frequently blocked by fixture bootstrap timeouts and did not reliably reach test execution; this keeps startup checks aligned with infrastructure readiness rather than endpoint-specific behavior.
**Impact:** Targeted commands now execute reliably and expose real test outcomes (pass/fail) instead of pre-test bootstrap cancellations.

### 2026-07-12T00:00:00Z: Integration tests migration alignment for HEAD contract classes
**By:** Bishop
**What:** Completed migration alignment of HEAD-focused integration tests to the shared assembly fixture serialization path by replacing manual JSON payload construction with `AppointmentInfo`/`AttendeeInfo` model payloads posted through `AgendaApplicationFixture.ApiJsonSerializerOptions`.
**Why:** Keep integration test setup behavior consistent with the assembly fixture as single source of truth and reduce serializer drift risk across test classes.
**Impact:** HEAD integration tests now consume the same serializer and ID/date conversion conventions as the rest of the integration suite.

### 2026-07-05T00:00:00Z: User directive - Team documents in English
**By:** Cyrille NDOUMBE (via Copilot)
**What:** All team-written documents must be authored in English.
**Why:** User request - captured for team memory.

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

### 2026-07-05T00:00:00Z: Integration tests robustness plan baseline and sequencing
**By:** Lambert
**What:** Define an execution-ready robustness plan for integration tests with explicit prioritization (quick wins, 1-2 sprint actions, long-term), CI anti-flake strategy, metrics, and a ticket backlog with DoD.
**Why:** Align team execution on a single anti-flake roadmap and reduce intermittent CI failures through measurable, staged implementation.
**Impact:** Shared baseline for delivery planning, quality gating, and weekly flakiness trend tracking across backend and test ownership.

### 2026-07-12T11:40:00Z: Integration migration validation pass
**By:** Hicks
**What:**
- Search integration tests were aligned with current auth behavior by sending explicit Bearer tokens on protected GET/HEAD `/appointments` requests.
- Shared token caching was added in `AgendaApplicationFixture.IssueAccessTokenAsync` to prevent repeated Keycloak password-grant logins across test classes.
- Full integration project validation now passes (`33/33`) with no `[Collection(...)]` usages.
**Why:**
- The previous `5/33` failures were functional auth regressions (`401`) plus token mint instability under repeated runs, not assembly fixture startup/teardown flakiness.
- Caching and explicit auth keep the assembly fixture migration stable while preserving minimal test-scope changes.

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction
