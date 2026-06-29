# Project Context

- **Project:** agenda
- **Created:** 2026-04-24
- **Requested By:** vscode
- **Description:** Appointment management microservice with vertical-slice architecture
- **Tech Stack:** .NET backend, PostgreSQL/SQLite datastore, Angular microfrontend

## Core Context

Agent Vasquez initialized and ready for work.

## Recent Updates

📌 Team initialized on 2026-04-24

## Learnings

Initial setup complete.

### 2026-06-06: Frontend callback redirect security review

- Reviewed the frontend Keycloak callback flow and flagged an intermediate redirect handling concern.
- Final implementation resolves the concern by enforcing safe relative redirect targets with fallback to `/`.
- Security review outcome: no open redirect finding remains on the finalized callback behavior.

### 2026-06-29: Issue 623 Phase 1 — Endpoint security inventory

- Audited all 6 FastEndpoints endpoints in `src/Agenda.API/Features/`.
- 4 endpoints carry `AllowAnonymous()`, bypassing the FallbackPolicy: POST /appointments (create), GET|HEAD /appointments (search), GET|HEAD /appointments/{id} (getById), PATCH /appointments/{id} (update).
- 2 mutations without auth (POST, PATCH) are classified as critical gaps — any unauthenticated client can create or modify appointments.
- 2 read endpoints without auth (GET /appointments, GET /appointments/{id}) are classified as elevated gaps pending data-sensitivity assessment.
- `DELETE /appointments/{id}/attendees/{attendeeId}` has no explicit annotation — protected by FallbackPolicy only, no role restriction (inconsistent with `DELETE /appointments/{id}` which requires `agenda-admin`).
- `/openapi/*` and `/scalar/*` are intentionally public per Bishop's decisions (2026-05-29).
- Full inventory written to `docs/plans/issue-623-endpoint-inventory.md`.
- Decision written to `.squad/decisions/inbox/vasquez-issue623-phase1-inventory.md`.