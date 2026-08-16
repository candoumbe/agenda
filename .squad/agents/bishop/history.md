# Project Context

- **Project:** agenda
- **Created:** 2026-04-24
- **Requested By:** vscode
- **Description:** Appointment management microservice with vertical-slice architecture
- **Tech Stack:** .NET backend, PostgreSQL/SQLite datastore, Angular microfrontend

## Core Context

Agent Bishop initialized and ready for work.

## Recent Updates

📌 Team initialized on 2026-04-24
📌 Null-attendees normalization decision centralized in `.squad/decisions.md` on 2026-04-24
📌 Logged AssemblyFixture migration completion and silent-success verification on 2026-05-26
📌 Routed by Scribe for issue #545 backend implementation batch on 2026-05-24

## Learnings

Initial setup complete.
- Pour le flow UI de planification, accepter `attendees: null` et normaliser en liste vide côté endpoint évite un blocage d'intégration sans changer la route ni le contrat principal.
- Le pattern de normalisation doit rester explicite et couvert par tests pour eviter les regressions frontend/backend.
- Pour les endpoints de listing, `total` doit representer le nombre total de pages cote API si le frontend pilote sa navigation avec ce champ; exposer aussi `totalCount` et `pageSize` evite les ambiguites de contrat.
- Les liens de pagination (`previous`/`next`/`last`) ne doivent jamais dependre du nombre d'elements de la page courante; ils doivent dependre du nombre total de pages calcule a partir de `totalCount` et `pageSize`.
- Le support du filtre `location` dans la recherche doit rester preserve dans les liens de pagination pour garantir la coherence de navigation.
- AppHost startup can fail before compilation if `global.json` pins an SDK that is not installed locally; aligning the pinned SDK to an available patch/feature band is a minimal and safe unblocker when `rollForward` is already configured.
- For GET/HEAD parity work, centralizing navigation headers in a shared response interceptor prevents header drift across slices and reduces maintenance cost.
- For scheduling UI flows, accepting `attendees: null` and normalizing to an empty list at endpoint level prevents integration blockers without changing route contracts.
- Keep normalization patterns explicit and covered by tests to prevent frontend/backend regressions.
- For listing endpoints, `total` must represent total pages when frontend pagination relies on this field; exposing `totalCount` and `pageSize` avoids contract ambiguity.
- Pagination links (`previous`/`next`/`last`) must be computed from total pages derived from `totalCount` and `pageSize`, never from current-page item count.
- Preserve active filters (including `location`) in pagination links to keep navigation coherent.
- AppHost startup can fail before compilation when `global.json` pins an unavailable SDK; aligning to an available patch/feature band is a minimal unblocker when `rollForward` is already configured.
- Reusing one response interceptor for both `Browsable<T>` and `PageOf<T>` keeps `GET`/`HEAD` metadata behavior consistent and prevents header drift between slices.
- Replacing Swagger UI with Scalar in FastEndpoints can stay minimal by keeping `SwaggerDocument(...)` for OpenAPI generation, switching runtime exposure to `UseOpenApi(...)` + `MapScalarApiReference(...)`, and updating launch URLs from `/swagger` to `/scalar`.
## Learning — 2026-05-27T19:56:18Z: Aspire health check endpoint binding
`WithHttpHealthCheck("/health")` without `endpointName` defaults to the first endpoint declared in `launchSettings.json` (HTTPS first in this repo). On a CI Linux runner without ASP.NET Core dev cert, that probe fails and — combined with `WaitForResourceHealthyAsync` — blocks integration test bootstrap. Always pass `endpointName: "http"` for Aspire health checks in integration test scenarios. Context: PR #546 diagnostic with Hicks.

## Learning — 2026-05-28T00:00:00Z: Single-entry/single-exit refactor in response interceptor
For `AddLinkHeaderResponseInterceptor`, a safe single-entry/single-exit refactor can be applied by replacing early returns with explicit guard booleans and structured branching in `InterceptResponseAsync`, while keeping helper behavior and header output unchanged. Targeted xUnit class filtering via MTP-compatible arguments (`-- --filter-class`) validates behavior without broad test execution.

## Learning — 2026-05-28T20:09:00Z: HATEOAS link query parameter case mismatch risk
When backend-generated pagination links use PascalCase query names (for example `Page`, `PageSize`) and frontend extraction expects lowercase keys, browser `URLSearchParams` returns null because key lookup is case-sensitive. Frontend parsing should check both variants where compatibility is required to prevent pagination state drift.

## Learning — 2026-06-01T08:43:38Z: Aspire connection-refused triage needs current-run endpoint verification
Intermittent `connection refused` reports during local Aspire startup can be caused by stale endpoint URLs after dynamic port changes between runs. A reliable triage sequence is: verify AppHost process outcome (`dotnet run` exit status), then probe only the currently advertised frontend/API/identity/messaging endpoints. In the investigated run, endpoint probes succeeded when URLs matched the active process output.

## Learning — 2026-07-12T00:00:00Z: HEAD integration tests should use fixture serializer end-to-end
When HEAD contract tests need setup data creation, prefer creating `AppointmentInfo`/`AttendeeInfo` payloads and posting with `fixture.ApiJsonSerializerOptions` instead of building raw JSON strings manually. This keeps serialization behavior aligned with the rest of the integration suite and avoids drift in NodaTime/ID conversion behavior between test classes.

## Learning — 2026-07-12T00:00:00Z: xUnit v3 filtering in this repository uses MTP extension arguments
For `Agenda.API.IntegrationTests`, class targeting should use the MTP/xUnit v3 style `dotnet test ... -- --filter-class "Namespace.Class"` rather than `--filter`. In the current environment, integration runs can still abort before test execution when Aspire health checks cannot reach Keycloak/API endpoints.

## Learning — 2026-07-12T00:00:00Z: Search HEAD integration contract now requires authenticated probes
`SearchAppointmentHeadContractShould` can fail with 401 when GET/HEAD calls are sent without a bearer token. A minimal and stable fix is to mint an `alice` token with `IssueAccessTokenAsync(...)` and attach `Authorization: Bearer` on the GET/HEAD requests under test, while keeping setup POST payload serialization unchanged.

## Learning — 2026-08-16T00:00:00Z: Aspire `https+http://` authority breaks JwtBearer when RequireHttpsMetadata is hardcoded
`AddKeycloakJwtBearer` composes the authority using the Aspire service-discovery scheme `https+http://`. `RequireHttpsMetadata` is hardcoded `true` outside Development (`src/Agenda.API/ServiceCollectionExtensions.cs:112`), and `JwtBearerPostConfigureOptions` requires `MetadataAddress` to start with `https://`. The result is an `InvalidOperationException` raised by `UseAuthentication` (`Program.cs:98`) on **every** request in `Production`/`Staging` — including `/alive`, which makes it look like a total startup failure rather than an auth misconfiguration. Scalar itself is fine: in `Development`, `/scalar/v1` and `/openapi/v1.json` both return 200 with assets served locally (no CDN dependency).

Two things made this much harder to see than it should have been:
- The AppHost does not set `ASPNETCORE_ENVIRONMENT`, so containers silently default to `Production` — straight onto the failing path.
- `builder.Services.AddSerilog()` is called with no argument (`Program.cs:53`), which silences all logging. Always pass a configured logger; a no-arg call turns a diagnosable failure into a black box.
- `TimedOutboxSweeper` lets an exception escape and kills the process (exit 139); background services need their own exception boundary.

Triage rule learned: when *every* route including the health endpoint returns 500, suspect middleware built at startup (auth, options post-configuration) before suspecting any individual feature.
