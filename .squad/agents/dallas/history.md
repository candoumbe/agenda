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
