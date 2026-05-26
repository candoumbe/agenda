# Plan : Réécriture de l'infrastructure des tests d'intégration

> **Issue de référence :** [#548 — 💪🏾Rewrite Aspire integration tests to avoid flaky tests](https://github.com/candoumbe/agenda/issues/548)
> **Auteur du plan :** Ripley (Lead)
> **Date :** 2026-05-25
> **Statut :** En cours

> **Mise à jour 2026-05-26 :** la stratégie de partage de fixture est désormais alignée sur **xUnit v3 AssemblyFixture** (registration assembly-level), pas sur un pattern collection fixture.

---

## Contexte

L'infrastructure actuelle des tests d'intégration est trop fragile et génère des tests instables (_flaky_).
Chaque classe de test démarre son propre AppHost Aspire (et donc ses propres conteneurs Docker), ce qui
entraîne des temps d'exécution excessifs, des conflits de ressources et de la duplication de code.

---

## Diagnostic de l'existant

| # | Problème | Localisation | Impact |
|---|----------|-------------|--------|
| 1 | Chaque classe de test démarre **son propre AppHost + conteneurs Docker** via `IAsyncLifetime` | `CreateAppointmentEndpointShould`, `HealthCheckEndpointShould`, `SearchAppointmentQueryBindingShould` | Lent, flaky, coûteux |
| 2 | `JsonSerializerOptions` entièrement dupliqué dans chaque classe (constructeur statique de 15+ lignes) | `CreateAppointmentEndpointShould` | Divergence silencieuse possible à chaque nouveau test |
| 3 | Logique de retry manuelle dans les tests (`ExecuteCreateRequestWithTransientInfrastructureRetryAsync`) | `CreateAppointmentEndpointShould` | Masque les vraies causes de flakiness ; la résilience est déjà configurée via `AddStandardResilienceHandler()` |
| 4 | `AgendaApplicationTestingBuilder` : rôle hybride (fixture + builder + waiter + disposer) | `Fixtures/` | Responsabilités mélangées, complexité croissante |
| 5 | Aucun contrôle de parallélisme xUnit (`parallelizeTestCollections` absent du `xunit.runner.json`) | `xunit.runner.json` | Risque de plusieurs AppHosts simultanés |
| 6 | `GetByIdEndpointShould` et `SearchAppointmentEndpointShould` entièrement commentés | `Appointments/v1/` | Régression silencieuse — ces scénarios ne sont plus testés |

---

## Architecture cible

```
tests/Agenda.API.IntegrationTests/
  Fixtures/
    AgendaApplicationFixture.cs        ← IAsyncLifetime partagé (1 seule instance par suite)
    AgendaApplicationTestingBuilder.cs ← Conservé, simplifié (utilisé par AppHostShould)
    DistributedApplicationTestingBuilderFactory.cs ← Conservé tel quel
  GlobalRegistrations.cs               ← Enregistrement AssemblyFixture xUnit v3
  AppHostShould.cs                     ← Consomme la fixture d'assembly partagée
  HealthCheckEndpointShould.cs         ← Migré : injecte AgendaApplicationFixture
  Appointments/v1/Create/
    CreateAppointmentEndpointShould.cs ← Migré : injecte AgendaApplicationFixture
  Appointments/v1/GetById/
    GetByIdEndpointShould.cs           ← Réécrit (actuellement commenté)
  Appointments/v1/Search/
    SearchAppointmentEndpointShould.cs ← Réécrit (actuellement commenté)
    SearchAppointmentQueryBindingShould.cs ← Migré : injecte AgendaApplicationFixture
```

**Principe clé** : une seule instance d'AppHost Aspire pour toute la suite de tests, partagée via
`[assembly: AssemblyFixture(typeof(AgendaApplicationFixture))]` (xUnit v3).

---

## Étapes

### Étape 1 — Créer `AgendaApplicationFixture`

- [x] Créer `Fixtures/AgendaApplicationFixture.cs` implémentant `IAsyncLifetime`
- [x] La fixture encapsule `DistributedApplicationTestingBuilderFactory.CreateBuilderAsync()` + `StartAsync()`
- [x] Exposer `HttpClient ApiClient { get; private set; }` (pré-configuré avec résilience + adresse de base)
- [x] Exposer `JsonSerializerOptions ApiJsonSerializerOptions { get; }` (NodaTime, tous les converters, camelCase)
- [x] Transférer la logique `WaitUntilApiIsReachableAsync` dans la fixture (ou la déléguer au builder existant)

> **Référence :** [Manage the AppHost — aspire.dev](https://aspire.dev/testing/manage-app-host/)

---

### Étape 2 — Enregistrer la fixture au niveau assembly (xUnit v3)

- [x] Ajouter l’enregistrement assembly-level de la fixture dans `GlobalRegistrations.cs` :
  ```csharp
  [assembly: AssemblyFixture(typeof(AgendaApplicationFixture))]
  ```

> **Référence :** [Write your first tests — aspire.dev](https://aspire.dev/testing/write-your-first-test/)

---

### Étape 3 — Mettre à jour `xunit.runner.json`

- [x] Ajouter `"parallelizeTestCollections": false` dans `xunit.runner.json`

```json
{
  "$schema": "https://xunit.net/schema/current/xunit.runner.schema.json",
  "methodDisplay": "method",
  "methodDisplayOptions": "all",
  "shadowCopy": false,
  "parallelizeTestCollections": false
}
```

---

### Étape 4 — Migrer `HealthCheckEndpointShould`

- [x] Remplacer `IAsyncLifetime` par l'injection de `AgendaApplicationFixture` dans le constructeur
- [x] Supprimer les champs `_appHost` et l'implémentation de `InitializeAsync()` / `DisposeAsync()`
- [x] Utiliser `fixture.ApiClient` à la place de `_client`

---

### Étape 5 — Migrer `CreateAppointmentEndpointShould`

- [x] Remplacer `IAsyncLifetime` par l'injection de `AgendaApplicationFixture` dans le constructeur
- [x] Supprimer les champs `_appHost`, `_sut` et l'implémentation de `InitializeAsync()` / `DisposeAsync()`
- [x] Supprimer le constructeur statique `s_jsonSerializerOptions` → utiliser `fixture.ApiJsonSerializerOptions`
- [x] Supprimer `ExecuteCreateRequestWithTransientInfrastructureRetryAsync` et `ShouldRetryBecauseOfTransientInfrastructureFailureAsync` → appel HTTP direct (`_client.PostAsJsonAsync(...)`)
- [x] Supprimer les constantes de retry devenues inutiles (`s_transientInfrastructureMaxAttempts`, etc.)

---

### Étape 6 — Migrer `SearchAppointmentQueryBindingShould`

- [x] Remplacer `IAsyncLifetime` par l'injection de `AgendaApplicationFixture` dans le constructeur
- [x] Supprimer les champs `_appHost` et l'implémentation de `InitializeAsync()` / `DisposeAsync()`
- [x] Utiliser `fixture.ApiClient` à la place de `_client`

---

### Étape 7 — Réécrire `GetByIdEndpointShould`

Ce fichier est entièrement commenté. Le réécrire avec la nouvelle infrastructure.

- [x] Décommenter et adapter la classe pour utiliser `AgendaApplicationFixture`
- [x] Implémenter le scénario **404 quand l'ID n'existe pas**
- [x] Implémenter le scénario **200 avec la ressource quand l'ID existe** (créer un rendez-vous au préalable puis le récupérer par son ID)
- [x] Valider les liens HATEOAS retournés (présence de `self`, URLs absolues)

> **Référence :** [Accessing resources in tests — aspire.dev](https://aspire.dev/testing/accessing-resources/)

---

### Étape 8 — Réécrire `SearchAppointmentEndpointShould`

Ce fichier est entièrement commenté. Le réécrire avec la nouvelle infrastructure.

- [x] Décommenter et adapter la classe pour utiliser `AgendaApplicationFixture`
- [x] Implémenter le scénario **résultat vide** (page 1, pageSize 10, 0 résultats)
- [x] Implémenter le scénario **résultat avec données** (créer un rendez-vous, vérifier qu'il apparaît dans la recherche)

> **Note :** Chaque test doit être indépendant des données laissées par d'autres tests. Utiliser des
> données suffisamment uniques (dates, sujets) pour éviter les interférences.

---

### Étape 9 — Validation finale

- [ ] Exécuter `./build.sh integration-tests` : tous les tests passent
- [ ] Vérifier qu'un seul AppHost est démarré pour la suite (via les logs de conteneurs Docker)
- [ ] Vérifier que `AppHostShould` reste stable avec le cycle de vie partagé de la fixture d'assembly
- [ ] Valider sur CI (push sur la branche de travail, vérifier les GitHub Actions)

---

## Ce qui ne change pas

| Élément | Raison |
|---------|--------|
| `AppHostShould` | Vérifie la disponibilité du client API exposé par la fixture d'assembly |
| `DistributedApplicationTestingBuilderFactory` | Conservé et utilisé par `AgendaApplicationFixture` et `AppHostShould` |
| `DistributedApplicationExtensions` | Conservé tel quel |
| Organisation des dossiers `v1/Feature/` | Aucun changement structurel |

---

## Bénéfices attendus

| Mesure | Avant | Après |
|--------|-------|-------|
| Instances AppHost par suite | N (1 par classe de test) | 1 |
| Temps de démarrage | ~120s × N | ~120s × 1 |
| Lignes de boilerplate par classe | ~20 | ~2 (constructeur) |
| Retry manuel dans les tests | Oui | Non (`AddStandardResilienceHandler`) |
| Tests `GetById` et `Search` | Commentés (désactivés) | Actifs et fonctionnels |

---

## Références

- [Write your first tests](https://aspire.dev/testing/write-your-first-test/)
- [Manage the AppHost](https://aspire.dev/testing/manage-app-host/)
- [Advanced testing scenarios](https://aspire.dev/testing/advanced-scenarios/)
- [Accessing resources in tests](https://aspire.dev/testing/accessing-resources/)
