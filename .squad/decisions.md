# Squad Decisions

## Active Decisions

### 2026-08-16T00:00:00Z: Ticket "scalar-fails-to-start-in-azurelinux-image" is misnamed — real cause is Keycloak/Aspire scheme incompatibility
**By:** Ripley, Bishop (diagnostic), recorded by Scribe
**What:**
- The ticket title is wrong on both counts. It is **not** a Scalar issue and **not** an Azure Linux issue.
- The published image `ghcr.io/candoumbe/agenda.api:0.2-scalar-fails-to-start-in-azurelinux-image.b724dd7` is based on `mcr.microsoft.com/dotnet/aspnet:10.0` (Ubuntu 24.04), and does contain commit `b724dd7`. The tag quoted in the ticket had a typo (double dot).
- Scalar works fully in `Development`: `/scalar/v1` → 200, `/openapi/v1.json` → 200, assets served locally with no CDN dependency.
- **Real root cause:** `AddKeycloakJwtBearer` builds the authority with the Aspire service-discovery scheme `https+http://`, while `RequireHttpsMetadata` is hardcoded `true` outside Development (`src/Agenda.API/ServiceCollectionExtensions.cs:112`). `JwtBearerPostConfigureOptions` requires `MetadataAddress` to start with `https://`, so `UseAuthentication` (`Program.cs:98`) throws `InvalidOperationException` on **every** request in `Production`/`Staging` — including `/alive`.
- **Aggravating:** the AppHost does not set `ASPNETCORE_ENVIRONMENT`, so containers default to `Production` and land directly on the failing path.
- **Secondary:** `builder.Services.AddSerilog()` is called with no argument (`Program.cs:53`), silencing all logs and hiding the failure. `TimedOutboxSweeper` throws an unhandled exception that kills the process (exit 139).
**Why:** Two independent investigations (image/runtime analysis and empirical run with real postgres + rabbitmq + migrations) converged on the same conclusion. Keeping the misleading title would send future work down the wrong path (image base, Scalar assets, CDN) instead of the authentication configuration.
**Impact:** Remediation must target the Keycloak authority scheme / `RequireHttpsMetadata` coupling, AppHost environment propagation, and Serilog configuration — not Scalar or the container base image. The ticket should be renamed accordingly.

### 2026-08-04T00:00:00Z: Number wrapper OpenAPI tests aligned with schema transformer wiring
**By:** Hicks
**What:** Replaced legacy NumberTypeMapper unit-test expectations with NumberTypeSchemaTransformer-focused tests that assert integer OpenAPI schema behavior for `PositiveInteger` and `NonNegativeInteger` (`type`, `format`, `minimum`, `maximum`) and verify `Program.cs` uses `AddSchemaTransformer` registrations for these wrappers.
**Why:** API documentation generation now relies on schema transformers, so tests must validate the active configuration path and avoid duplicated legacy mapper expectations.

### 2026-07-05T00:00:00Z: User directive - Team documents in English
**By:** Cyrille NDOUMBE (via Copilot)
**What:** All team-written documents must be authored in English.
**Why:** User request - captured for team memory.

### 2026-05-29T13:32:53Z: User directive - Link markdown docs using markdown links
**By:** vscode (via Copilot)
**What:** In markdown files, references to other markdown files must use markdown links.
**Why:** User request - captured for team memory.

### 2026-05-29T07:08:11Z: User directive - Present results as dashboard/table
**By:** Cyrille NDOUMBE (via Copilot)
**What:** Going forward, present results in a dashboard/table format.
**Why:** User request - captured for team memory.

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

### 2026-04-26T09:54:11Z: User directive - Agent history language
**By:** Cyrille NDOUMBE (via Copilot)
**What:** All agent history files must always be written in English.
**Why:** Ensure consistency and readability across all team history artifacts.

### 2026-04-28T16:08:01Z: User directive - Reinforce GitFlow and CONTRIBUTING
**By:** Cyrille NDOUMBE (via Copilot)
**What:** Team must strictly follow GitFlow and read `CONTRIBUTING.md` before any code modification.
**Why:** Reinforce workflow and contribution discipline as non-optional preconditions for code changes.

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

### 2026-05-28T01:12:06Z: User directive - Always use single entry/single exit when coding
**By:** Cyrille NDOUMBE (via Copilot)
**What:** When the team writes code, always use the single entry / single exit approach.
**Why:** User request - captured for team memory.

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction
