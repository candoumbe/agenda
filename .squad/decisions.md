# Squad Decisions

## Active Decisions

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

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction
