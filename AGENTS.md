# AGENTS

This file defines repository-specific instructions for AI agents working on this codebase.

## Repository purpose

- Agenda is a REST API for appointment management.
- The repository mainly contains .NET 10 projects under [src/](src/) and tests under [tests/](tests/).
- [src/Agenda.API](src/Agenda.API/) uses FastEndpoints and a feature-oriented organization.
- [src/Agenda.Frontend](src/Agenda.Frontend/) is a separate Angular application with its own editing conventions.

## General rules for agents

- ✅ Do read [README.md](README.md) and [CONTRIBUTING.md](CONTRIBUTING.md) before making any non-trivial change.
  Example to follow: read the contribution rules before adding a new endpoint or changing build behavior.
  ❌ Do not start restructuring code based only on assumptions.

- ✅ Do keep changes small, focused, and consistent with the existing code style.
  Example to follow: update the validator and its related tests without reformatting unrelated files.
  ❌ Do not mix the requested fix with unrelated refactors.

- ✅ Do fix the root cause whenever possible.
  Example to follow: fix the namespace or endpoint placement that breaks an architecture test instead of disabling the test.
  ❌ Do not add a local workaround that hides the real problem.

- ✅ Do limit edits to files that are directly relevant to the task.
  Example to follow: if the change is in [src/Agenda.API](src/Agenda.API/), avoid touching frontend files unless the task explicitly requires it.
  ❌ Do not rewrite nearby files just because they look outdated.

- ✅ Do add or update tests for every observable behavior change.
  Example to follow: when changing request validation, update the validator tests and the endpoint tests if needed.
  ❌ Do not ship behavior changes without test coverage.

- ✅ Do update documentation when public behavior changes.
  Example to follow: update [README.md](README.md) or [docs/](docs/) if an endpoint contract, workflow, or setup step changes.
  ❌ Do not leave public-facing behavior undocumented.

## Backend architecture

- ✅ Do preserve the vertical-slice architecture used by [src/Agenda.API](src/Agenda.API/).
  Example to follow: keep all code for an appointment feature grouped by feature instead of splitting it into generic controller or service folders.
  ❌ Do not reorganize backend code into technical layers that conflict with the current structure.

- ✅ Do place every FastEndpoints endpoint in a namespace matching `Agenda.API.Features.*`.
  Example to follow: put a create-appointment endpoint under a namespace such as `Agenda.API.Features.Appointments.v1.Create`.
  ❌ Do not place endpoints in generic namespaces such as `Agenda.API.Endpoints` or `Agenda.API.Controllers`.

- ✅ Do keep one endpoint per functional namespace.
  Example to follow: create a dedicated namespace for each endpoint instead of grouping multiple endpoint classes together.
  ❌ Do not place several endpoint classes in the same feature namespace if the architecture tests expect isolation.

- ✅ Do keep the request type in the same namespace as an endpoint derived from `Endpoint<,>`.
  Example to follow: store `CreateAppointmentRequest` beside the matching endpoint in the same namespace.
  ❌ Do not define the request in a shared DTO namespace if the endpoint uses `Endpoint<,>`.

- ✅ Do check [tests/Agenda.API.ArchitecturalTests/VerticalSliceArchitectureTests.cs](tests/Agenda.API.ArchitecturalTests/VerticalSliceArchitectureTests.cs) before creating, moving, or restructuring endpoints.
  Example to follow: verify namespace rules first, then implement the endpoint in a way that satisfies the architectural tests.
  ❌ Do not assume the architecture constraints from memory when the test file is the source of truth.

## C# code style

- ✅ Do follow the root [.editorconfig](.editorconfig) strictly.
  Example to follow: keep indentation, naming, brace placement, and `using` placement aligned with the existing C# files.
  ❌ Do not introduce a personal formatting style.

- ✅ Do use 4-space indentation in C# files.
  Example to follow: align blocks and object initializers with 4 spaces.
  ❌ Do not use tabs or 2-space indentation in backend files.

- ✅ Do use explicit types instead of `var`, except for anonymous types.
  Example to follow: write `ValidationResult validationResult = ...` instead of `var validationResult = ...`.
  ❌ Do not use `var` for normal locals just because the type is obvious.

- ✅ Do keep `using` directives outside namespaces.
  Example to follow: place all `using` statements at the top of the file before the namespace declaration.
  ❌ Do not move `using` directives inside the namespace block.

- ✅ Do respect the existing naming conventions.
  Example to follow: use PascalCase for public types, prefix interfaces with `I`, use `_fieldName` for private fields, and `s_fieldName` for private static readonly fields.
  ❌ Do not introduce naming such as `m_field`, `camelCaseType`, or unprefixed interfaces.

- ✅ Do prefer readable control flow with one entry and one exit when it remains reasonable.
  Example to follow: compute the result in local variables and return once at the end of the method when the logic is non-trivial.
  ❌ Do not add multiple early returns throughout a complex method if that makes the logic harder to review.

- ✅ Do stay consistent with the surrounding code even if another style could also work.
  Example to follow: match the style already used in the file you are editing.
  ❌ Do not modernize syntax opportunistically if that creates inconsistency inside the same project.

## Test style

- ✅ Do align new backend tests with the existing xUnit style.
  Example to follow: use the same overall structure and assertion style already used in the target test project.
  ❌ Do not introduce a different testing framework or a radically different test layout.

- ✅ Do name test classes with the `Should` suffix.
  Example to follow: name a validator test class `CreateAppointmentRequestValidatorShould`.
  ❌ Do not use names such as `CreateAppointmentRequestValidatorTests` if the surrounding project consistently uses `Should`.

- ✅ Do keep the explicit Arrange, Act, Assert style when it already exists in the file.
  Example to follow: preserve the existing AAA comments and section ordering in unit tests.
  ❌ Do not compress an AAA test into an overly terse style if that breaks local consistency.

- ✅ Do reuse the testing patterns already present in the repository.
  Example to follow: use `TheoryData`, `MemberData`, `Bogus`, `AwesomeAssertions` or `FluentAssertions`, and helpers from [tests/Agenda.UnitTests.Helpers](tests/Agenda.UnitTests.Helpers/) when those patterns already fit the scenario.
  ❌ Do not invent new test utilities when the repository already provides the needed helpers.

- ✅ Do choose the right test level based on the impact of the change.
  Example to follow: add unit tests for validators or mappers, integration tests for HTTP behavior, and architecture tests for structural changes.
  ❌ Do not rely on a single unit test when the change affects routing, API contracts, or endpoint placement.

## Frontend

- ✅ Do follow [src/Agenda.Frontend/.editorconfig](src/Agenda.Frontend/.editorconfig) for frontend changes.
  Example to follow: keep the frontend formatting aligned with the Angular project conventions.
  ❌ Do not apply backend formatting rules to frontend files.

- ✅ Do use 2-space indentation in frontend files.
  Example to follow: format TypeScript, HTML, and related frontend files with 2 spaces.
  ❌ Do not reindent frontend code to 4 spaces.

- ✅ Do keep single quotes in TypeScript files.
  Example to follow: write `import { Component } from '@angular/core';`.
  ❌ Do not switch frontend TypeScript code to double quotes unless the file already requires it for a specific reason.

- ✅ Do validate frontend changes with `npm run build` and `npm run test -- --watch false` from [src/Agenda.Frontend](src/Agenda.Frontend/) when applicable.
  Example to follow: run both commands after changing Angular components or frontend configuration.
  ❌ Do not claim the frontend is validated if those checks were not run.

## Validation before completion

- ✅ Do prefer `./build.sh Tests` from the repository root for full validation.
  Example reference: [build.sh](build.sh)
  Example to follow: use the main pipeline command when the change touches several areas of the repository.
  ❌ Do not stop at a partial check if the change spans backend, tests, and frontend.

- ✅ Do run at least the affected test projects for a targeted backend change.
  Example to follow: run the relevant unit or integration test project after editing an API validator or endpoint.
  ❌ Do not rely only on compilation when behavior changed.

- ✅ Do run [tests/Agenda.API.ArchitecturalTests](tests/Agenda.API.ArchitecturalTests/) for API endpoint structure changes.
  Example to follow: execute the architecture test project after moving or adding a FastEndpoints endpoint.
  ❌ Do not skip architecture tests when namespaces or endpoint placement changed.

- ✅ Do report validation honestly.
  Example to follow: state exactly which commands were executed and which were not.
  ❌ Do not imply that a test, build, or validation step was run when it was not.

## Useful commands

- ✅ Full test pipeline: `./build.sh Tests` (script: [build.sh](build.sh))
  ❌ Avoid using this as a placeholder in your summary if you did not actually run it.

- ✅ API architecture tests only: `dotnet test tests/Agenda.API.ArchitecturalTests/Agenda.API.ArchitecturalTests.csproj` (project: [tests/Agenda.API.ArchitecturalTests/Agenda.API.ArchitecturalTests.csproj](tests/Agenda.API.ArchitecturalTests/Agenda.API.ArchitecturalTests.csproj))
  ❌ Do not claim endpoint structure is valid without running the architecture tests after structural API changes.

- ✅ Targeted backend tests: `dotnet test <path-to-csproj>`
  ❌ Do not run an unrelated test project and present it as sufficient validation.

- ✅ Frontend build: run `npm run build` from [src/Agenda.Frontend](src/Agenda.Frontend/)
  ❌ Do not run the build from the repository root and assume it validated the Angular app.

- ✅ Frontend tests: run `npm run test -- --watch false` from [src/Agenda.Frontend](src/Agenda.Frontend/)
  ❌ Do not leave watch mode enabled in automation-oriented validation.

## What an agent must avoid

- ❌ Do not break the vertical-slice organization by moving code into technical-layer folders.
  ✅ Preferred behavior: keep feature code grouped by feature and version.

- ❌ Do not introduce a new naming, formatting, or test style that conflicts with the repository.
  ✅ Preferred behavior: match the conventions already enforced by [.editorconfig](.editorconfig), existing code, and existing tests.

- ❌ Do not add a dependency or architectural pattern without checking that it fits the current projects and tests.
  ✅ Preferred behavior: verify that the new dependency integrates with the existing build, architecture, and test setup before adopting it.

- ❌ Do not omit tests for an observable change.
  ✅ Preferred behavior: add or update the most relevant tests before considering the task complete.