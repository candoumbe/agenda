# Plan: Integration Tests Assembly Fixture Migration

> Date: 2026-05-26
> Scope: tests/Agenda.API.IntegrationTests
> Status: Proposed

## 1) Goal And Constraints

- [ ] Migrate integration tests to a single shared fixture lifecycle using xUnit v3 assembly fixture support.
- [ ] Keep the plan practical and incremental so tests remain runnable during migration.
- [ ] Preserve current test behavior and endpoint coverage while reducing duplicated startup code.
- [ ] Keep test code in English and aligned with existing repository conventions.
- [ ] Explicit rule: `CollectionAttribute` is not used for this migration.

## 2) Technical Approach (Native xUnit v3 Assembly Fixture)

- [ ] Use xUnit v3 native assembly fixture support (`[assembly: AssemblyFixture(typeof(...))]`) as the primary mechanism.
- [ ] Centralize app host startup and shared `HttpClient`/serializer setup in one fixture.
- [ ] Inject shared resources through fixture types and constructor dependencies, not through test collections.
- [ ] Keep fixture responsibilities focused: start app host, expose clients/options, dispose resources cleanly.

## 3) Step-By-Step Migration Checklist

- [ ] Create `AgendaAssemblyFixture` (or equivalent) in `tests/Agenda.API.IntegrationTests/Fixtures/` implementing async lifecycle.
- [ ] Add assembly-level registration for the fixture in the integration test assembly.
- [ ] Move shared startup/bootstrap logic from per-class setup into the assembly fixture.
- [ ] Move shared serializer/client configuration into the fixture (single source of truth).
- [ ] Remove collection-based setup attributes/usages from migrated tests.
- [ ] Migrate each integration test class from class-local startup (`IAsyncLifetime`) to fixture-based dependency usage.
- [ ] Keep `AppHostShould` isolated only if its scenario explicitly requires independent lifecycle validation.
- [ ] Remove obsolete retry/helpers that only compensated for duplicate startup instability.
- [ ] Run targeted tests after each migrated test class to detect regressions early.
- [ ] Update any docs/comments that still reference collection fixtures as the main pattern.

## 4) Risks And Mitigations

- [ ] Risk: runner/tooling mismatch for xUnit v3 assembly fixture behavior.
  - Mitigation: pin and verify test SDK/runner compatibility before full migration.
- [ ] Risk: hidden coupling between tests due to shared lifecycle.
  - Mitigation: keep test data unique and avoid mutable global state.
- [ ] Risk: migration temporarily breaks one or more integration test classes.
  - Mitigation: migrate class-by-class with targeted test runs and small commits.
- [ ] Risk: disposal/startup leaks from centralized fixture.
  - Mitigation: enforce deterministic startup/teardown and validate resource cleanup in logs.

## 5) Validation Checklist (Commands)

- [ ] Restore and build solution:
  - `dotnet restore Agenda.sln`
  - `dotnet build Agenda.sln -c Debug`
- [ ] Run integration tests project:
  - `dotnet test tests/Agenda.API.IntegrationTests/Agenda.API.IntegrationTests.csproj -c Debug --no-build`
- [ ] Run repository integration pipeline target:
  - `./build.sh integration-tests`
- [ ] Optional full test safety net:
  - `./build.sh Tests`
- [ ] Confirm no collection-based attribute usage remains in migrated integration tests:
  - `rg "\[Collection\(" tests/Agenda.API.IntegrationTests`

## 6) Fallback If Runner Blocks Assembly Fixture Support

- [ ] If assembly fixture support is blocked by runner/tooling constraints, use a temporary shared bootstrap pattern with one explicit helper fixture class and no `CollectionAttribute`.
- [ ] Keep the same fixture API surface so switching back to native assembly fixture later is low-risk.
- [ ] Track fallback usage in this plan/status and define a return condition: upgrade runner/tooling, then re-enable assembly fixture registration.
- [ ] Re-run the validation checklist after fallback activation and after returning to native assembly fixture.
