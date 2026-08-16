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

### Issue #502 Documentation Review (2026-04-25)
- Frontend feature implementation has excellent model documentation (JSDoc on all interfaces) but lacks component-level header comments.
- Complex algorithms (date grouping, chronological sorting) benefit from inline documentation even when method names are self-documenting.
- CHANGELOG entries are properly categorized and issue-referenced; README updates should be considered for new user-facing features.
- Team should establish a pattern: models/contracts get detailed JSDoc; complex methods in components get inline explanation of algorithm logic.
- Documentation checklists work well: verify CHANGELOG, README, models, inline comments, type exports, and setup requirements independently.
## Learning — 2026-08-16T00:00:00Z: Verify ticket premises against the actual image before diagnosing
Ticket `scalar-fails-to-start-in-azurelinux-image` asserted an Azure Linux base image. Inspection of `ghcr.io/candoumbe/agenda.api:0.2-scalar-fails-to-start-in-azurelinux-image.b724dd7` showed the base is `mcr.microsoft.com/dotnet/aspnet:10.0` (Ubuntu 24.04), and the image did contain commit `b724dd7` — the tag quoted in the ticket simply had a typo (double dot). Reliable triage order: (1) confirm the tag actually resolves, (2) confirm the base image and the commit it carries, (3) only then reproduce the crash. Here the container died **before** `app.Run()` because Aspire-supplied connection strings were absent, which is a composition problem, not an OS or Scalar problem. Ticket titles are hypotheses, not evidence.
