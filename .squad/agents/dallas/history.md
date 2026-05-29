# Project Context

- **Project:** agenda
- **Created:** 2026-04-24
- **Requested By:** vscode
- **Description:** Appointment management microservice with vertical-slice architecture
- **Tech Stack:** .NET backend, PostgreSQL/SQLite datastore, Angular microfrontend

## Core Context

Agent Dallas initialized and ready for work.

## Recent Updates

📌 Team initialized on 2026-04-24
📌 Scheduling contract alignment decision captured in centralized decisions log on 2026-04-24

## Learnings

Initial setup complete.
- L'API de creation d'un rendez-vous attend `POST /appointments` avec des dates ISO et des participants utilisant la propriete `phoneNumber`.
- Le frontend Angular reposait sur le template starter; la navigation a ete recentree sur un flux dedie `appointments/new` avec formulaire reactive et gestion d'etats UX (loading/succes/erreur).
- Les specs frontend de ce projet tournent avec Vitest, donc il faut utiliser `vi.fn()` plutot que le namespace Jasmine.
- Les decisions cross-agent doivent etre remontees dans `.squad/decisions.md` pour la traçabilite d'equipe.
- La recherche des rendez-vous doit etre declenchee par des appels API debounces a la saisie (`subject`) pour garantir une pagination correcte cote serveur.
- Lors d'une recherche par sujet, il faut reinitialiser `currentPage` a 1 avant l'appel API pour eviter des pages incoherentes sur un nouveau filtre.
- Sur la liste des rendez-vous, la page affichee doit rester synchronisee avec `response.page` (source de verite API), sinon l'UI peut annoncer une page differente de la page reellement servie.
- Dans ce frontend Angular zoneless, un callback asynchrone de chargement peut necessiter un `ChangeDetectorRef.detectChanges()` apres mise a jour des signaux pour garantir un rendu immediat sans interaction utilisateur.
- Pour une pagination fiable, exploiter d'abord `links.last/next/previous` pour les bornes et la navigation, puis utiliser `response.total` en repli si les liens sont absents.
- La recherche multi-criteres du listing doit envoyer `subject`, `location`, `from`, `to` (dates en ISO) et reinitialiser la page a 1 a chaque changement de filtre.
- Pour une coherence durable UI/API, la page affichee doit toujours etre resynchronisee depuis `response.page` et non seulement depuis l'etat local.
- For the appointments listing, the default date interval should be initialized on first load to [now, now + 15 days] and reused when clearing filters.
- To support "jump to first incoming appointment" without backend changes, the frontend can issue a focused follow-up query (`page=1`, `pageSize=1`, `from=<current to>`) when the selected interval has no result.
- The empty-state UX for interval searches should explicitly include the selected bounds and expose contextual actions (create appointment and jump to next available window).

### 2026-05-23: Issue #541 frontend delivery update

- Implemented issue #541 behavior in appointments listing with default 15-day interval initialization and empty-range fallback discovery query.
- Added jump-to-first-incoming flow that shifts the interval to the discovered appointment start and reloads list data.
- Frontend validation completed with successful build and test execution for the updated behavior.

### 2026-05-28: Pagination + HATEOAS header alignment

- The Angular `ApiService` now reads pagination/HATEOAS metadata from response headers (`Link`, `total`, `count`) and merges it with the legacy body contract.
- Link parsing must support both `rel="previous"` and `rel="prev"` and normalize to the frontend `links.previous` property for stable list-page navigation.
- For robustness, total pages should prioritize HATEOAS-derived values (`links.last` or `total` header + page size) before the legacy `body.total` value.
- Updated files: `src/Agenda.Frontend/src/services/api-service.ts`, `src/Agenda.Frontend/src/services/api-service.spec.ts`, `src/Agenda.Frontend/src/models/page-of.ts`.
- Pagination link query parsing should support both camelCase and PascalCase key variants (`page`/`Page`) because browser URL parsing is case-sensitive while backend-generated links may use PascalCase.

### 2026-05-28: HEAD counter, Homepage, Attendees stub

- Added `countAppointments(params)` to `ApiService` — makes HEAD to `/api/appointments`, returns `Observable<number>` from `total` response header; returns 0 when header absent.
- Added `totalResultsCount = signal<number | null>(null)` and `hasCountError = signal(false)` to `AppointmentsListPageComponent`; HEAD fires in parallel with GET inside `loadAppointments` (not sequential).
- The results count badge is always visible in the list page (loading state → "Chargement…", error → "Résultats non disponibles", resolved → "X résultat(s) trouvé(s)").
- Created `HomePageComponent` (3 navigation cards — primary gradient to `/appointments`, accent gradient to `/appointments/new`, tertiary gradient to `/attendees`); route `''` now points to the homepage instead of a redirect.
- Created `AttendeesSearchPageComponent` stub — search form (name + email) renders with a "Fonctionnalité à venir" placeholder; route `/attendees` registered in `app.routes.ts`.
- All existing tests remain green (30/30 list-page, 13/13 api-service); new tests added: 2 for HEAD counter in list-page spec, 10 for homepage spec (100 % coverage on homepage).
- When adding `countAppointments` to the component spy, update the spy type declaration AND the `beforeEach` mock in the spec file — otherwise existing tests fail at runtime.

### 2026-05-29: Schedule appointment cancel navigation

- Added a cancel action to the schedule appointment page with conditional confirmation based on unsaved form input.
- Updated the component, template, stylesheet, and spec together to keep the UX and test coverage aligned.
- Validation reported by Dallas: targeted schedule-appointment-page test passed and `npm run build` passed.
- Delivery commit: `ec44a3ce1a1e00c8ca01e592ab76fea4757d1a60`.
