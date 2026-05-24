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
📌 Routed by Scribe for issue #545 backend implementation batch on 2026-05-24

## Learnings

Initial setup complete.
- For scheduling UI flows, accepting `attendees: null` and normalizing to an empty list at endpoint level prevents integration blockers without changing route contracts.
- Keep normalization patterns explicit and covered by tests to prevent frontend/backend regressions.
- For listing endpoints, `total` must represent total pages when frontend pagination relies on this field; exposing `totalCount` and `pageSize` avoids contract ambiguity.
- Pagination links (`previous`/`next`/`last`) must be computed from total pages derived from `totalCount` and `pageSize`, never from current-page item count.
- Preserve active filters (including `location`) in pagination links to keep navigation coherent.
- AppHost startup can fail before compilation when `global.json` pins an unavailable SDK; aligning to an available patch/feature band is a minimal unblocker when `rollForward` is already configured.
- Reusing one response interceptor for both `Browsable<T>` and `PageOf<T>` keeps `GET`/`HEAD` metadata behavior consistent and prevents header drift between slices.