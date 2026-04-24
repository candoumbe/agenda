# Project Context

- **Project:** agenda
- **Created:** 2026-04-24
- **Requested By:** vscode
- **Description:** Appointment management microservice with vertical-slice architecture
- **Tech Stack:** .NET backend, PostgreSQL/SQLite datastore, Angular microfrontend

## Core Context

Agent Ripley initialized and ready for work.

## Recent Updates

📌 Team initialized on 2026-04-24
📌 Final review insights synchronized into session and decisions logs on 2026-04-24

## Learnings

Initial setup complete.
- Pour le flux de planification, aligner les specs Angular sur Vitest (`vi.fn`) et normaliser `attendees: null` en liste vide côté endpoint permet de stabiliser frontend+backend sans changer le contrat de route.
- La consolidation finale est plus fiable quand les decisions sont dedupliquees avant archivage.