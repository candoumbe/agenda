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

## Learnings

Initial setup complete.
- Pour le flow UI de planification, accepter `attendees: null` et normaliser en liste vide côté endpoint évite un blocage d'intégration sans changer la route ni le contrat principal.
- Le pattern de normalisation doit rester explicite et couvert par tests pour eviter les regressions frontend/backend.