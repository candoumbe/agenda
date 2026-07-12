# Plan: Integration Tests Assembly Fixture Migration

> Date: 2026-05-26
> Scope: tests/Agenda.API.IntegrationTests
> Status: In Progress

## Latest Validation Snapshot (2026-07-12)

- Full integration project run passed after migration fixes: `33 passed / 0 failed`.
- Remaining failures from the previous full run (`5 failed / 33`) were traced to missing authenticated requests on search GET/HEAD integration tests.
- Search integration tests now use authenticated requests aligned with current API behavior and the shared assembly fixture token-issuance path.
- `CollectionAttribute` check remains clean: no occurrences in `tests/Agenda.API.IntegrationTests`.
- `./build.sh integration-tests` is currently blocked before test execution by a pipeline bootstrap issue:
  `Could not inject value for IHaveGitRepository.GitRepository` with duplicate key `github-pr-owner-number`.

## 1) Goal And Constraints

- [x] Migrate integration tests to a single shared fixture lifecycle using xUnit v3 assembly fixture support.
- [ ] Keep the plan practical and incremental so tests remain runnable during migration.
- [ ] Preserve current test behavior and endpoint coverage while reducing duplicated startup code.
- [ ] Keep test code in English and aligned with existing repository conventions.
- [ ] Explicit rule: `CollectionAttribute` is not used for this migration.

## 2) Technical Approach (Native xUnit v3 Assembly Fixture)

- [x] Use xUnit v3 native assembly fixture support (`[assembly: AssemblyFixture(typeof(...))]`) as the primary mechanism.
- [x] Centralize app host startup and shared `HttpClient`/serializer setup in one fixture.
- [x] Inject shared resources through fixture types and constructor dependencies, not through test collections.
- [x] Keep fixture responsibilities focused: start app host, expose clients/options, dispose resources cleanly.

## 3) Step-By-Step Migration Checklist

- [x] Create `AgendaAssemblyFixture` (or equivalent) in `tests/Agenda.API.IntegrationTests/Fixtures/` implementing async lifecycle.
- [x] Add assembly-level registration for the fixture in the integration test assembly.
- [x] Move shared startup/bootstrap logic from per-class setup into the assembly fixture.
- [x] Move shared serializer/client configuration into the fixture (single source of truth).
- [x] Remove collection-based setup attributes/usages from migrated tests.
- [x] Migrate each integration test class from class-local startup (`IAsyncLifetime`) to fixture-based dependency usage.
- [x] Keep `AppHostShould` isolated only if its scenario explicitly requires independent lifecycle validation.
- [x] Remove obsolete retry/helpers that only compensated for duplicate startup instability.
- [x] Run targeted tests after each migrated test class to detect regressions early.
- [x] Update any docs/comments that still reference collection fixtures as the main pattern.

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
- [x] Run integration tests project:
  - `dotnet test tests/Agenda.API.IntegrationTests/Agenda.API.IntegrationTests.csproj -c Debug --no-build`
- [ ] Run repository integration pipeline target:
- [ ] Run repository integration pipeline target (attempted, blocked before target execution by build bootstrap issue):
  - `./build.sh integration-tests`
- [ ] Optional full test safety net:
  - `./build.sh Tests`
- [x] Confirm no collection-based attribute usage remains in migrated integration tests:
  - `rg "\[Collection\(" tests/Agenda.API.IntegrationTests`

## 6) Fallback If Runner Blocks Assembly Fixture Support

- [ ] If assembly fixture support is blocked by runner/tooling constraints, use a temporary shared bootstrap pattern with one explicit helper fixture class and no `CollectionAttribute`.
- [ ] Keep the same fixture API surface so switching back to native assembly fixture later is low-risk.
- [ ] Track fallback usage in this plan/status and define a return condition: upgrade runner/tooling, then re-enable assembly fixture registration.
- [ ] Re-run the validation checklist after fallback activation and after returning to native assembly fixture.
