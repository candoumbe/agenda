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

## Learning — 2026-06-29T00:00:00Z: Phase 2 issue-623 — Suppression AllowAnonymous endpoints appointments

Lorsqu'une FallbackPolicy `RequireAuthenticatedUser` est déjà configurée au niveau de `AddAuthorization`, supprimer `AllowAnonymous()` sur les endpoints FastEndpoints suffit à les protéger — aucune annotation `[Authorize]` supplémentaire n'est nécessaire. Les 4 endpoints appointments (Create, Search/List, GetById, Patch) avaient un `AllowAnonymous()` incorrect qui contournait silencieusement cette politique. Le seul `AllowAnonymous` légitime restant est sur `MapScalarApiReference` dans `Program.cs` (documentation publique intentionnelle).

Fichiers modifiés :
- `src/Agenda.API/Features/Appointments/v1/Create/CreateAppointmentEndpoint.cs`
- `src/Agenda.API/Features/Appointments/v1/Search/SearchAppointmentsEndpoint.cs`
- `src/Agenda.API/Features/Appointments/v1/GetById/GetAppointmentByIdEndpoint.cs`
- `src/Agenda.API/Features/Appointments/v1/Update/PatchAppointmentByIdEndpoint.cs`

## Learning — 2026-06-29T12:00:00Z: Phase 2 issue-623 — Régression tests intégration après suppression AllowAnonymous

Après la suppression de `AllowAnonymous()` des 4 endpoints appointments, les tests d'intégration existants (`CreateAppointmentEndpointShould`, `GetByIdEndpointShould`, `SearchAppointmentEndpointShould`, etc.) échouaient avec 401 car `ApiClient` n'avait pas de token.

**Correction centralisée dans `AgendaApplicationTestingBuilder`** (un seul fichier modifié) :
1. `KeycloakClient` créé avant `ApiClient` pour que `IssueAccessTokenAsync` soit disponible dès l'init.
2. Après `WaitUntilApiIsReachableAsync`, appel à `IssueAccessTokenAsync("alice", "password", ...)` et injection du token dans `ApiClient.DefaultRequestHeaders.Authorization`.
3. Probe readiness `WaitUntilApiIsReachableAsync` changée de `/appointments?...` vers `/health` (public via `.AllowAnonymous()` dans ServiceDefaults) pour éviter le 401 pendant le démarrage.

`AnonymousApiClient` reste inchangé — préserve le comportement des tests d'auth négatifs de Hicks. Aucun test individuel modifié.

Fichier modifié : `tests/Agenda.API.IntegrationTests/Fixtures/AgendaApplicationTestingBuilder.cs`
Validation : `dotnet build tests/Agenda.API.IntegrationTests/` — **Build succeeded, 0 Error(s)**.

## Learning — 2026-06-29T12:00:00Z: Issue #623 regression — Integration tests fixed after AllowAnonymous removal

After Phase 2 removed `AllowAnonymous()` from the 4 appointment endpoints, existing integration tests (`CreateAppointmentEndpointShould`, `GetByIdEndpointShould`, `SearchAppointmentEndpointShould`) failed with 401. The readiness probe `WaitUntilApiIsReachableAsync` also blocked because it polled `GET /appointments?…` which now requires auth.

**Fix (centralized in `AgendaApplicationTestingBuilder.StartAsync`):**
1. `KeycloakClient` created before `ApiClient` so `IssueAccessTokenAsync` is available immediately.
2. Real Keycloak token obtained via `IssueAccessTokenAsync("alice", "password", …)` after readiness, injected into `ApiClient.DefaultRequestHeaders.Authorization`.
3. Readiness probe changed from `GET /appointments?…` → `GET /health` (public endpoint via `.AllowAnonymous()` in ServiceDefaults).

`AnonymousApiClient` is unaffected — Hicks' negative auth tests continue to work correctly. No individual test files modified.  
Regression resolved in session `20260629T120000Z-issue623-phase1-2-4`.
