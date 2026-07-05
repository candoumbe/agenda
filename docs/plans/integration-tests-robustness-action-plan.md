# Integration Test Robustness Action Plan

## Context
This document formalizes a concrete plan to reduce integration test flakiness across [docs/plans/integration-tests-infrastructure-rewrite.md](docs/plans/integration-tests-infrastructure-rewrite.md) and [docs/plans/integration-tests-assembly-fixture-migration-plan.md](docs/plans/integration-tests-assembly-fixture-migration-plan.md), while taking into account the current runner configuration in [tests/Agenda.API.IntegrationTests/xunit.runner.json](tests/Agenda.API.IntegrationTests/xunit.runner.json).

## 1) Objectives
- Stabilize integration test execution with a target success rate >= 98% over 30 consecutive CI runs.
- Remove structural flakiness causes (concurrency, multiple AppHost startups, masking retries).
- Reduce the average integration pipeline duration by 20% without reducing functional coverage.
- Standardize an operating mode that is testable, observable, and maintainable by the team.

## 2) Observed issues (flakiness)
- Active collection-level parallelism (`parallelizeTestCollections: true`, `maxParallelThreads: 2`) increases contention risk on AppHost/containers.
- Historical pattern of multiple AppHost startups per test class, causing resource conflicts and timing variability.
- Presence/remnants of retry patterns inside tests that can mask infrastructure defects (instead of addressing root causes).
- Historically fragile coverage on some endpoint scenarios (tests commented out then rewritten), with risk of silent regression.
- Missing anti-flake CI metrics baseline (no explicit instability budget and no dedicated gate).

## 3) Prioritized action plan

### Quick wins (1-2 days)
1. Disable xUnit collection parallelism for the integration suite (`parallelizeTestCollections: false`).
2. Verify/enforce use of a single assembly fixture for the AppHost lifecycle.
3. Inventory and remove unjustified test retries (or replace them with explicit deterministic waits).
4. Add an `integration-stability-smoke` CI job that runs 5 serial iterations of the critical suite.
5. Produce a baseline report (run time, green rate, top intermittent errors).

### Mid term (1-2 sprints)
1. Complete migration of all classes to the shared fixture infrastructure.
2. Isolate test data (unique naming/timestamp/data namespace) to remove inter-test collisions.
3. Consolidate precondition setup helpers (deterministic seed, robust cleanup).
4. Add explicit infrastructure readiness assertions before running sensitive endpoint scenarios.
5. Stabilize search/GetById tests with independent datasets and strict verification criteria.

### Long term
1. Set up a flakiness dashboard (weekly trend) with degradation alerts.
2. Add automatic quarantine for detected unstable tests (with a correction SLA).
3. Integrate periodic burn-in campaigns (repeated runs) outside the main pipeline.
4. Establish a robust quality gate: block merges if the flake budget is exceeded.

## 4) Actionable backlog (proposed tickets)

| ID | Action | Value | Effort | Suggested owner | Dependencies | Definition of Done (DoD) |
|---|---|---|---|---|---|---|
| IT-ROB-001 | Switch [tests/Agenda.API.IntegrationTests/xunit.runner.json](tests/Agenda.API.IntegrationTests/xunit.runner.json) to non-parallel collection mode | Quickly reduces contention and false negatives | S | Hicks (Tester) | None | PR merged, CI green, serialized run evidence in logs |
| IT-ROB-002 | Audit and remove non-essential test retries | Exposes real flake root causes | M | Hicks (Tester) + Bishop (Backend) | IT-ROB-001 | No custom retry helper remains outside documented exceptions |
| IT-ROB-003 | Verify AppHost bootstrap uniqueness through assembly fixture | Lifecycle stability and startup cost reduction | M | Bishop (Backend) | IT-ROB-001 | Single AppHost instance observed per suite, documentation updated |
| IT-ROB-004 | Create `integration-stability-smoke` CI job (5 serial runs) | Early detection of intermittent regressions | M | Ripley (Lead) + Hicks (Tester) | IT-ROB-001 | Job added, visible in pipeline, success rate published |
| IT-ROB-005 | Standardize generation of unique test data | Eliminates inter-test collisions | M | Bishop (Backend) | IT-ROB-003 | Shared utilities applied to critical tests |
| IT-ROB-006 | Define anti-flake metrics and thresholds (budget) | Quantified steering and decision support | S | Ripley (Lead) | IT-ROB-004 | Thresholds documented and used as a quality gate |
| IT-ROB-007 | Add a weekly flakiness trend dashboard | Continuous visibility and proactive action | M | Lambert (Docs/DevRel) + Hicks (Tester) | IT-ROB-006 | Dashboard published, weekly ritual established |
| IT-ROB-008 | Implement a quarantine mechanism for unstable tests | Avoids blocking the main flow while enforcing fixes | L | Ripley (Lead) + Hicks (Tester) | IT-ROB-006 | Process documented, pipeline supports it, correction SLA active |

## 5) Anti-flake CI strategy
- Run the critical integration suite serially in a dedicated stage (no collection parallelism).
- Add a repeat-run mode (for example, 5 passes) on target branch and critical PRs.
- Automatically capture and publish:
  - success rate per test,
  - average/percentile run time,
  - intermittent error signatures.
- Introduce a progressive gate:
  - phase 1: warning if success rate < 98%,
  - phase 2: merge blocked if < 97% over a rolling 30-run window.
- Keep a diagnostics channel (AppHost log artifacts + startup events) for fast root cause analysis.

## 6) Success metrics
- Global flake rate: < 2% over 30 consecutive executions.
- CI re-run rate: -50% within 2 sprints.
- Median integration pipeline duration: -20%.
- Number of tests in quarantine: downward trend sprint after sprint.
- Flakiness MTTR (detection -> fix merged): < 2 business days.

## 7) Risks and mitigations
- Risk: longer CI time after serialization.
  - Mitigation: target critical suites first, optimize fixture setup, monitor duration.
- Risk: retry removal exposes many real failures at once.
  - Mitigation: progressive rollout by test batch, daily root cause triage.
- Risk: unintended coupling between tests sharing the fixture.
  - Mitigation: strict data isolation, deterministic cleanup, dedicated QA reviews.
- Risk: partial adoption of the plan (return to old practices).
  - Mitigation: mandatory checklist in integration PR template + Lead/Tester review.

## 8) Two-sprint execution plan

| Sprint | Target | Actions | Expected outcome |
|---|---|---|---|
| Sprint 1 | Immediate stabilization | IT-ROB-001, IT-ROB-002, IT-ROB-003, IT-ROB-004 | More stable integration suite, metrics baseline available |
| Sprint 2 | Industrialization | IT-ROB-005, IT-ROB-006, IT-ROB-007 (+ IT-ROB-008 framing) | Active anti-flake governance, weekly instrumented tracking, quality gate defined |

## Implementation checklist
- [ ] Validate the scope of impacted critical tests (Lead + Tester).
- [ ] Apply the non-parallel runner configuration for collections.
- [ ] Remove/explicitly justify each remaining test retry.
- [ ] Verify single AppHost lifecycle per suite and document evidence.
- [ ] Enable the stability CI job with repeated runs.
- [ ] Publish baseline metrics, then target thresholds.
- [ ] End of Sprint 1 review: adjust backlog based on dominant observed causes.
- [ ] End of Sprint 2 review: activate anti-flake gate and quarantine plan.
