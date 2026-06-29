# Issue 623 — Endpoint Security Inventory

**Author:** Vasquez (Security Engineer)
**Date:** 2026-06-29
**Branch:** `feature/restrict-api-access-to-authenticated-users-only`
**Scope:** Phase 1 — Exhaustive audit of all FastEndpoints endpoints and auth/authz configuration

---

## Auth/Authz Infrastructure Summary

| Component | File | Behavior |
|---|---|---|
| `AddCustomAuthentication` | [ServiceCollectionExtensions.cs](../../src/Agenda.API/ServiceCollectionExtensions.cs) | JWT Bearer (Keycloak RS256), `FallbackPolicy = RequireAuthenticatedUser` |
| `RealmRolesClaimsTransformation` | `src/Agenda.API/Authentication/` | Maps Keycloak realm roles to `ClaimTypes.Role` |
| `UseAuthentication` / `UseAuthorization` | [Program.cs](../../src/Agenda.API/Program.cs) line 88–89 | Standard ASP.NET Core middleware pipeline |
| OpenAPI branch | [Program.cs](../../src/Agenda.API/Program.cs) line 85–86 | `MapWhen` isolates `/openapi/*` **before** auth middleware — effectively public |
| Scalar UI | [Program.cs](../../src/Agenda.API/Program.cs) line 134 | `.AllowAnonymous()` — explicitly public |
| FastEndpoints role claim type | [Program.cs](../../src/Agenda.API/Program.cs) | `config.Security.RoleClaimType = ClaimTypes.Role` — cohérent avec la transformation Keycloak |

---

## Inventaire Complet des Endpoints

| # | Classe | Namespace | Méthode(s) | Route | Attribut(s) de sécurité | Exposition actuelle | Gap identifié |
|---|---|---|---|---|---|---|---|
| 1 | `CreateAppointmentEndpoint` | `Agenda.API.Features.Appointments.v1.Create` | `POST` | `/appointments` | `AllowAnonymous()` | **PUBLIC** | ⚠️ OUI — mutation de données sans authentification |
| 2 | `DeleteEndpoint` | `Agenda.API.Features.Appointments.v1.Delete` | `DELETE` | `/appointments/{id}` | `Roles("agenda-admin")` | **Rôle-restreint** | ✅ NON |
| 3 | `GetAppointmentByIdEndpoint` | `Agenda.API.Features.Appointments.v1.GetById` | `GET`, `HEAD` | `/appointments/{id}` | `AllowAnonymous()` | **PUBLIC** | ⚠️ OUI — lecture de données potentiellement sensibles sans authentification |
| 4 | `SearchAppointmentsEndpoint` | `Agenda.API.Features.Appointments.v1.Search` | `GET`, `HEAD` | `/appointments` | `AllowAnonymous()` | **PUBLIC** | ⚠️ OUI — listing paginé d'appointments exposé sans authentification |
| 5 | `PatchAppointmentByIdEndpoint` | `Agenda.API.Features.Appointments.v1.Update` | `PATCH` | `/appointments/{id}` | `AllowAnonymous()` | **PUBLIC** | 🔴 CRITIQUE — modification de données sans authentification |
| 6 | `RemoveAnAttendeeByItsIdEndpoint` | `Agenda.API.Features.Appointments.v1.ManageAttendees.RemoveAttendee` | `DELETE` | `/appointments/{id}/attendees/{attendeeId}` | *(aucun)* | **Protégé** (FallbackPolicy) | ⚠️ PARTIEL — authentification requise mais aucune restriction par rôle |

### Endpoints d'infrastructure (hors FastEndpoints)

| Surface | Route | Exposition | Justification | Gap |
|---|---|---|---|---|
| OpenAPI JSON | `/openapi/{documentName}.json` | **PUBLIC** | `MapWhen` branch avant le pipeline auth | ✅ Intentionnel — doc publique (décision Bishop 2026-05-29) |
| Scalar UI | `/scalar/*` | **PUBLIC** | `.AllowAnonymous()` explicite | ✅ Intentionnel — décision Bishop 2026-05-29 |

---

## Gaps Prioritaires

### 🔴 Critique — Mutations sans authentification

#### `PATCH /appointments/{id}` — `PatchAppointmentByIdEndpoint`
- **Risque :** N'importe quel client non authentifié peut modifier n'importe quel appointment (titre, horaires, participants).
- **Impact :** Altération de données, potentiel déni de service applicatif, contournement de toute logique de propriété des données.
- **Action :** Retirer `AllowAnonymous()` et laisser la FallbackPolicy s'appliquer (minimum), ou restreindre au rôle `agenda-user`/`agenda-admin` selon la décision métier.

#### `POST /appointments` — `CreateAppointmentEndpoint`
- **Risque :** N'importe quel client non authentifié peut créer des appointments, potentiellement en masse (spam, pollution de base, DoS applicatif).
- **Impact :** Pollution de la base de données, impossibilité d'attribuer un propriétaire à l'appointment créé, vecteur d'abus.
- **Action :** Retirer `AllowAnonymous()`. Minimum : utilisateur authentifié requis. Idéalement : rôle `agenda-user` ou `agenda-admin`.

### ⚠️ Élevé — Lecture sans authentification

#### `GET|HEAD /appointments/{id}` — `GetAppointmentByIdEndpoint`
- **Risque :** Les données d'appointment (participants, horaires, contexte) sont accessibles sans jeton. Si des données personnelles (noms, contacts) sont incluses dans la réponse, cela constitue une exposition RGPD.
- **Action :** Évaluer la sensibilité des données retournées. Retirer `AllowAnonymous()` si les appointments ne sont pas publics par nature.

#### `GET|HEAD /appointments` — `SearchAppointmentsEndpoint`
- **Risque :** Listing paginé de tous les appointments accessible publiquement, incluant des liens vers les ressources individuelles.
- **Action :** Même évaluation que GetById. Si les appointments ont un propriétaire, ajouter un filtre sur l'identité de l'appelant en plus de l'authentification.

### ⚠️ Partiel — Authentification sans contrôle de rôle

#### `DELETE /appointments/{id}/attendees/{attendeeId}` — `RemoveAnAttendeeByItsIdEndpoint`
- **Risque :** Tout utilisateur authentifié peut supprimer n'importe quel participant de n'importe quel appointment, sans vérification de propriété ni de rôle.
- **Comparaison :** `DeleteEndpoint` (suppression de l'appointment entier) est restreint au rôle `agenda-admin`. La suppression d'un participant devrait avoir une protection cohérente.
- **Action :** Ajouter `Roles("agenda-admin")` ou une politique de propriété selon le modèle métier.

---

## Endpoints Légitimement Publics

Ces surfaces sont intentionnellement publiques et documentées dans les décisions d'équipe :

| Surface | Justification |
|---|---|
| `GET /openapi/{documentName}.json` | Documentation technique publique — décision Bishop 2026-05-29 |
| `GET /scalar/*` | Interface Scalar de documentation — décision Bishop 2026-05-29, `.AllowAnonymous()` explicite |

Aucun endpoint FastEndpoints ne devrait rester dans cette liste après la Phase 2 (les décisions existantes ne mentionnent pas d'exposition volontaire des endpoints métier).

---

## Recommandations pour Bishop (Implémenteur)

### Priorité 1 — Retirer AllowAnonymous des endpoints mutants

Pour `POST /appointments` et `PATCH /appointments/{id}`, supprimer l'appel `AllowAnonymous()` dans `Configure()`. La FallbackPolicy `RequireAuthenticatedUser` s'appliquera automatiquement.

```csharp
// Avant
public override void Configure()
{
    Post("/appointments");
    AllowAnonymous(); // ← à supprimer
    ...
}

// Après
public override void Configure()
{
    Post("/appointments");
    // FallbackPolicy RequireAuthenticatedUser s'applique
    ...
}
```

Si une restriction par rôle est souhaitée (recommandé) :

```csharp
Roles("agenda-user", "agenda-admin");
```

### Priorité 2 — Évaluer et corriger les endpoints en lecture

Pour `GET /appointments` et `GET /appointments/{id}`, la décision de garder ou retirer `AllowAnonymous()` dépend du modèle de données :
- Si les appointments contiennent des données personnelles → retirer `AllowAnonymous()`.
- Si les appointments sont publics par conception → documenter explicitement la décision dans `.squad/decisions.md`.

**Recommandation Vasquez :** Retirer `AllowAnonymous()` par défaut (principe du moindre privilège), puis ré-exposer si besoin documenté.

### Priorité 3 — Homogénéiser la protection de RemoveAttendee

Aligner `RemoveAnAttendeeByItsIdEndpoint` avec `DeleteEndpoint` :
```csharp
public override void Configure()
{
    Delete("/appointments/{id}/attendees/{attendeeId}");
    Roles("agenda-admin"); // cohérence avec DeleteEndpoint
}
```

Ou définir une politique plus granulaire si un `agenda-user` peut retirer un participant de ses propres appointments.

### Priorité 4 — Couverture de tests

Pour chaque endpoint dont `AllowAnonymous()` est retiré, ajouter des tests d'intégration couvrant :
- `401 Unauthorized` sur appel sans jeton.
- `403 Forbidden` sur appel avec jeton valide mais rôle insuffisant (si restriction par rôle ajoutée).
- `2xx` sur appel avec jeton et rôle valides.

Le pattern est déjà établi dans les tests d'intégration existants pour `DELETE /appointments/{id}`.

---

## Matrice de risque résumée

| Endpoint | Authentification requise | Autorisation par rôle | Risque résiduel | Action requise |
|---|---|---|---|---|
| `POST /appointments` | ❌ | ❌ | 🔴 Critique | Retirer `AllowAnonymous()` |
| `PATCH /appointments/{id}` | ❌ | ❌ | 🔴 Critique | Retirer `AllowAnonymous()` |
| `GET /appointments` | ❌ | ❌ | ⚠️ Élevé | Évaluer puis retirer si données sensibles |
| `GET /appointments/{id}` | ❌ | ❌ | ⚠️ Élevé | Évaluer puis retirer si données sensibles |
| `DELETE /appointments/{id}/attendees/{attendeeId}` | ✅ (FallbackPolicy) | ❌ | ⚠️ Moyen | Ajouter restriction par rôle |
| `DELETE /appointments/{id}` | ✅ | ✅ (`agenda-admin`) | ✅ Faible | Aucune |
| `/openapi/*.json` | ❌ | ❌ | ✅ Acceptable | Intentionnel, documenté |
| `/scalar/*` | ❌ | ❌ | ✅ Acceptable | Intentionnel, documenté |
