# Project Context

- **Project:** agenda
- **Created:** 2026-04-24
- **Requested By:** vscode
- **Description:** Appointment management microservice with vertical-slice architecture
- **Tech Stack:** .NET backend, PostgreSQL/SQLite datastore, Angular microfrontend

## Core Context

Agent Hicks initialized and ready for work.

## Recent Updates

📌 Team initialized on 2026-04-24
📌 Validation support work referenced in session and orchestration logs on 2026-04-24

## Learnings

Initial setup complete.
- Les tentatives de validation sont plus utiles quand elles sont journalisees avec contexte et resultat attendu.

### 2026-04-25: Issue #502 Validation (Appointments List UI)

**Key Findings:**
- **Test Framework Mismatch (CRITICAL, FIXED):** Frontend project uses Vitest, not Jasmine. Component tests must use `vi.fn()` mocks, not `spyOn()` and `jasmine.createSpy()`. This prevented tests from compiling until fixed.
- **Locale Configuration:** Locale data must be registered and provided in TestBed when template uses locale-specific date formatting (French locale required for 'fr-FR' date pipe in this component).
- **Code Quality:** Excellent — proper separation of concerns, explicit types throughout, no debug statements, 86%+ coverage achieved on component.
- **Architecture Compliance:** Verified — backend SearchAppointmentsEndpoint follows vertical-slice pattern, architectural tests pass (2/2), API contract properly aligned with frontend models.

**Pattern for Vitest Angular Component Tests:**
```typescript
import { vi } from 'vitest';
// Use vi.fn().mockReturnValue() for service mocks
// Provide LOCALE_ID if template uses locale-specific pipes
// Import and registerLocaleData for non-English locales
```

**Validation Confidence:** Very High
- All 25 tests pass (12 component + 7 API service + 5 other files)
- Backend architecture tests (2/2) pass
- Code style fully compliant with project conventions
- API contract alignment verified against SearchAppointmentsEndpoint response
- Coverage: 83.42% overall, 86.33% on component, 88.37% branch coverage

### 2026-04-26: Unit-test Build Failure Diagnosis

**Task:** Diagnose why `./build.sh unit-tests` was failing in the Nuke pipeline.

**Root Causes Found (two compounding issues):**

1. **Format gate blocks UnitTests first.**
   The Nuke `UnitTests` target depends on `Format`. Any formatting violation causes the pipeline to abort before a single test runs. This masks test failures entirely when code isn't fully formatted.

2. **Nuke `DotNetTest` is incompatible with Microsoft.Testing.Platform (MTP).**
   The projects under `tests/` use MTP (via the `Microsoft.Testing.Platform` runner). Nuke's built-in `DotNetTest` helper passes `--logger` and `--results-directory` flags that MTP does not accept, causing the test host to exit with an error before any test assertion is evaluated. This is **not** a red test — it is a runner invocation mismatch.

**Key files:**
- `build/Build.cs` — Nuke target definitions, `DotNetTest` call site
- `tests.props` — shared test project properties (MTP enablement)
- `global.json` — SDK version pinning

**Required fixes:**
- Replace `DotNetTest(...)` in the `UnitTests` Nuke target with a direct MTP-compatible invocation (e.g., `DotNet("test {project} --no-build")` or a `ProcessTasks.StartProcess("dotnet", ...)` call that omits unsupported flags).
- Either decouple `UnitTests` from the `Format` dependency or document a `--skip Format` invocation for developers running tests locally.

**Confidence:** High — both blockers reproduced consistently; no ambiguity about root cause.

---

### 2026-04-25: UI Bugfixes QA Validation (Dallas's Changes)

**Changes Validated:**
1. **Appointment cards appearing immediately (Bug #1)**
   - Fix: ChangeDetectorRef.detectChanges() in finalize operator (line 77-81)
   - Ensures change detection after async load without user interaction
   - Implementation checks for destroyed ViewRef before calling detectChanges()

2. **Page number synchronization (Bug #2)**
   - Fix: currentPage.set(response.page) syncs display with API source of truth (line 88)
   - Previous fix: currentPage.set(1) when search changes (line 57)
   - Prevents stale UI state between clicks

**Test Results:**
- ✅ All 14 component tests PASS
- ✅ All 27 frontend tests PASS (no regressions)
- ✅ Architecture tests (2/2) PASS
- ✅ Coverage maintained at 96.2% component, 86.4% branch, 84.1% overall
- ⚠️ CSS budget: 5.4 KB (budget 4.0 KB, +265 bytes) - build warning only

**Code Quality:**
- ✅ No console.log or debug statements
- ✅ ChangeDetectorRef usage minimal and targeted
- ✅ Clean change detection logic, no performance concerns
- ✅ No unrelated changes mixed in

**Key Tests for Bugfixes:**
1. "should render appointment cards after async load without user interaction" → validates Fix #1
2. "should sync current page from API response and display it" → validates Fix #2
3. Server-side search + pagination reset verified

**Regression Testing:**
- Date grouping works
- Ongoing/upcoming badges render
- API error handling works
- Post-creation redirect flow preserved
- All 27 frontend tests still pass

**Recommendation:** ✅ **APPROVED** - Both bugfixes validated, no regressions, ready for merge

### 2026-04-26: Pagination metadata coherence validation

**Key Findings:**
- Frontend pagination confidence increases when tests assert link-driven behavior (`links.last`, `links.next`, `links.previous`) instead of trusting only `total`.
- Edge-case coverage should explicitly include `0` result, `1` result, and API page value above last-page metadata to prevent navigation drift.
- Multi-criteria search coverage is stronger when request assertions verify serialized ISO dates and all active filters (`subject`, `location`, `from`, `to`).

**Execution Notes:**
- Frontend suite passed with new edge-case scenarios (34/34).
- Targeted backend search tests could not complete in this environment because the Postgres fixture container failed to initialize (Docker runtime mount error), so backend validation remains partially blocked.

### 2026-04-26: Multi-criteria + pagination contract validation follow-up

**Key Findings:**
- Pagination assertions are more stable when tests verify link-driven navigation semantics before derived counters.
- Contract tests should keep explicit checks for `location` propagation in listing requests and preserved filter context across pagination links.

**Residual Risk:**
- Environment-level container initialization issues can still prevent full targeted backend execution in some contexts; architecture and frontend validations reduce but do not eliminate this risk.

### 2026-05-23: Issue #541 Frontend date-window behavior validation

**What was validated:**
- Added and stabilized appointments-list frontend tests for default 15-day interval initialization on first load.
- Added empty-state assertions for interval-scoped messaging and creation CTA visibility when no appointments exist in range.
- Added jump-to-first-incoming coverage to verify date-window repositioning and second search execution with a +15 day range.

**Important test pattern update:**
- Multi-criteria searches can trigger an additional fallback call (`pageSize: 1`, `from = selected to`) when the first interval response is empty, so assertions must not rely only on the last call being the main query.

**Execution result:**
- `npm run test -- --watch false` passed in `src/Agenda.Frontend` with 41/41 tests green.