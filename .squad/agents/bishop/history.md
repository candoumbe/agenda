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
- Pour les endpoints de listing, `total` doit representer le nombre total de pages cote API si le frontend pilote sa navigation avec ce champ; exposer aussi `totalCount` et `pageSize` evite les ambiguites de contrat.
- Les liens de pagination (`previous`/`next`/`last`) ne doivent jamais dependre du nombre d'elements de la page courante; ils doivent dependre du nombre total de pages calcule a partir de `totalCount` et `pageSize`.
- Le support du filtre `location` dans la recherche doit rester preserve dans les liens de pagination pour garantir la coherence de navigation.