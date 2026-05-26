# Intégration .NET Aspire — Plan d’actions détaillé

Objectif: ajouter le support .NET Aspire pour lancer facilement toute la stack Agenda (API + base de données + migrations), avec les contraintes suivantes:
- Les projets de migration de données sont séparés du projet d’API principal.
- Fournir un projet séparé pour déclencher la migration des bases.
- Les tests d’intégration s’appuient sur la nouvelle stack Aspire.
- La migration des données s’exécute automatiquement AVANT le démarrage de l’API.

Ce document décrit l’architecture cible, les projets à créer, les dépendances NuGet, le wiring Aspire, l’adaptation des tests d’intégration et les ajustements CI/CD.

---

## 1. Architecture cible

Composants principaux:
- Agenda.AppHost (Aspire AppHost): point d’entrée pour orchestrer ressources et services.
- Agenda.ServiceDefaults (lib partagée): conventions transverses (logging, health checks, OpenTelemetry, JSON, configuration).
- Agenda.Migrator (background service): exécute automatiquement les migrations EF Core des DbContext avant l’API.
- Agenda.API (existant): service web cible, configuré pour consommer la chaîne de connexion fournie par Aspire.
- Base de données Postgres (ressource container Aspire). Option de bascule SQLite en dev si souhaité.

Ordonnancement de démarrage :
1) Postgres démarre et devient healthy.
2) Agenda.Migrator s’exécute (Database.MigrateAsync()) et termine avec succès (exit code 0).
3) Agenda.API démarre et s’enregistre healthy.

---

## 2. Projets et packages à ajouter

### 2.1 Agenda.ServiceDefaults (nouveau, class library)
Objectif: regrouper les “defaults” à partager par les services (API, Migrator).

Packages recommandés :
- Microsoft.Extensions.Http
- Microsoft.Extensions.Hosting
- Microsoft.Extensions.Diagnostics.HealthChecks
- OpenTelemetry.Extensions.Hosting
- OpenTelemetry.Instrumentation.AspNetCore
- OpenTelemetry.Instrumentation.Http
- OpenTelemetry.Exporter.Otlp
- (Optionnel) Serilog.AspNetCore / Serilog.Sinks.Console

Contenu:
- Méthodes d’extension pour:
  - Ajouter logs/Serilog
  - Ajouter OpenTelemetry (traces/metrics/logs) avec ressources (service.name=Agenda.API/Agenda.Migrator).
  - Ajouter HealthChecks de base (/health/live, /health/ready pour l’API).
  - Config JSON (NodaTime, StronglyTypedId), si partage utile.

### 2.2 Agenda.Migrator (nouveau, background worker)
Objectif: déclencher uniquement les migrations EF Core puis s’éteindre.

Packages:
- Microsoft.EntityFrameworkCore.Design
- Microsoft.EntityFrameworkCore
- Npgsql.EntityFrameworkCore.PostgreSQL (provider principal)
- (Option) Microsoft.Data.Sqlite & Microsoft.EntityFrameworkCore.Sqlite pour dev local
- NodaTime + NodaTime.Serialization.SystemTextJson (si utilisé par DbContext)

Références projet:
- Agenda.DataStores (contient AgendaDataStore)
- Agenda.DataStores.Postgres (contient les Migrations pour Postgres)
- Agenda.ServiceDefaults (pour logging/OTel homogènes)

Code (Program.cs) — logique:
- Lire Provider (env: Provider, default: postgres)
- Lire ConnectionStrings__Agenda (ou --connection-string via args)
- Enregistre le background worker
- Enregistre AgendaDataStore selon provider:
  - Postgres: AddNpgsqlDbContext<AgendaDb>(..., b => b.MigrationsAssembly("Agenda.DataStores.Postgres").UseNodaTime())
  - Sqlite (optionnel): AddSqlite(..., b => b.MigrationsAssembly("Agenda.DataStores.Sqlite").UseNodaTime())
- Exit 0 si succès; exit != 0 sinon.

Code (MigratorWorker)

Ergonomie:
- Supporte variables d’env Aspire: ConnectionStrings__Agenda, Provider.
- Logs clairs pour étapes: connexion, découverte des migrations, application des scripts, durée.

### 2.3 Agenda.AppHost (nouveau, Aspire AppHost)
Objectif: orchestrer les ressources et assurer l’ordre « migrator avant API ».

Packages:
- Aspire.Hosting
- Aspire.Hosting.Testing (pour les tests)
- Aspire.Npgsql (si disponible) ou création d’un container générique Postgres

Déclaration des ressources (Program.cs):
- var builder = DistributedApplication.CreateBuilder(args);
- Postgres:
  - builder.AddPostgres("agenda-db", version: "16")
    .WithDataVolume()
    .WithEnvironment("POSTGRES_USER", "agenda")
    .WithEnvironment("POSTGRES_PASSWORD", "changeme")
    .WithEnvironment("POSTGRES_DB", "agenda");
  - Exposer une connection string nommée "Agenda".
- Migrator:
  - builder.AddProject<Projects.Agenda_Migrator>("migrator")
    .WithReference(postgres)
    .WithEnvironment("Provider", "postgres")
    .WithEnvironment("ConnectionStrings__Agenda", postgres.GetConnectionString())
    .AsStartupTask(); // tâche qui doit finir avec succès
- API:
  - builder.AddProject<Projects.Agenda_API>("api")
    .WithReference(postgres)
    .WithEnvironment("ConnectionStrings__Agenda", postgres.GetConnectionString())
    .WithExternalHttpEndpoints();
  - S’assurer que l’API « attend » la réussite du migrator (selon API Aspire: AsStartupTask() + dépendances implicites, ou .WaitFor(migrator)).

Health/Ready:
- Exposer health endpoints API et marquer l’AppHost comme healthy uniquement quand l’API est ready.

Option dev Sqlite:
- Alternative: builder.AddParameter("Provider", default: "postgres").
- Si Provider=sqlite, ne pas déclarer Postgres; passer une connection string file-based et lancer migrator en mode sqlite.

---

## 3. Modifications dans Agenda.API

- Supprimer la responsabilité de migration automatique au démarrage de l’API:
  - Retirer services.AddAsyncInitializer<DataStoreMigrateInitializerAsync<AgendaDataStore>>; (laisser un seed optionnel si besoin, mais idéalement seed aussi via migrator ou une StartupTask dédiée).
- S’appuyer strictement sur ConnectionStrings__Agenda fournie par Aspire/AppHost.
- Conserver health endpoints (/health/live, /health/ready).

---

## 4. Tests d’intégration via Aspire

But: exécuter les tests contre une stack représentative (API + DB) provisionnée par l’AppHost.

Dépendances tests:
- Aspire.Hosting.Testing
- Xunit/NUnit selon existant

Patron de base pour tests:
- Créer une classe AgendaAppFactory : DistributedApplicationFactory pour Agenda.AppHost
  - ConfigureResources(builder => { options de test si besoin; ports dynamiques; DB ephemeral; variables Provider/ConnectionStrings }).
  - Obtenir l’endpoint HTTP de l’API via factory.GetHttpClient("api").
- Adapter les tests existants pour utiliser AgendaAppFactory et HttpClient fourni.
- Les migrations seront déjà appliquées car migrator est une StartupTask dans le graphe.

Parallélisme:
- Si exécution en parallèle, envisager bases isolées par test (ex: suffixe db) ou re-création des schémas par lot d'exécution.

---

## 5. CI/CD

- S’assurer que l’agent CI dispose de Docker (pour Postgres) si Provider=postgres.
- Les tests d’intégration s’exécutent comme d’habitude (`dotnet test`), la factory se charge de démarrer le graphe.
- Conserver collecte de couverture et export des artefacts existants.
- Variables sensibles: utiliser GitHub Actions/DevOps secrets, ou .env locale, mappées vers AppHost.

---

## 6. Étapes concrètes (checklist)

1) Créer Agenda.ServiceDefaults (class lib)
   - Ajouter extensions Logging, OTel, HealthChecks.
2) Créer Agenda.Migrator (console)
   - Référencer Agenda.DataStores et provider Postgres/Sqlite.
   - Implémenter Program.cs pour appliquer les migrations.
3) Créer Agenda.AppHost (Aspire)
   - Déclarer Postgres, Migrator (StartupTask), API (dépendant du migrator), health.
   - Exposer ConnectionStrings__Agenda à Migrator + API.
4) Modifier Agenda.API
   - Supprimer l’initializer de migration automatique.
   - Vérifier la consommation de la connection string « Agenda ».
5) Adapter tests d’intégration
   - Ajouter DistributedApplicationFactory (Aspire.Hosting.Testing).
   - Réécrire la fixture de tests pour exposer HttpClient de l’API via l’AppHost.
6) CI
   - Vérifier Docker présent.
   - Lancer `dotnet test` (les tests démarrent la stack Aspire pour eux-mêmes).

---

## 7. Exemples de snippets

### 7.1 Program.cs du Migrator (extrait)
```csharp
HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddHostedService<MigrationWorker>();

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource(MigrationWorker.ActivitySourceName));

builder.AddNpgsqlDbContext<FindusContext>("postgres");

IHost host = builder.Build();
host.Run();
```

### 7.2 MigratorWorker.cs du Migrator (extrait)
```csharp
public class MigratorWorker(
    IServiceProvider serviceProvider,
    IHostApplicationLifetime hostApplicationLifetime) : BackgroundService
{

    internal const string ActivitySourceName = "Migrations";
    private static readonly ActivitySource s_activitySource = new(ActivitySourceName);


    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        using var activity = s_activitySource.StartActivity("Migrating database");

        try
        {
            using var scope = serviceProvider.CreateScope();
            AgendaDataStore dbContext = scope.ServiceProvider.GetRequiredService<AgendaDataStore>();

            await RunMigrationAsync(dbContext, cancellationToken);
        }
        catch (Exception ex)
        {
            activity?.AddException(ex);
            throw;
        }

        hostApplicationLifetime.StopApplication();
    }

    private static async Task RunMigrationAsync(DbContext dbContext, CancellationToken cancellationToken)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
                                    {
                                        // Run migration in a transaction to avoid partial migration if it fails.
                                        await dbContext.Database.MigrateAsync(cancellationToken);
                                    });
    }

}
```


### 7.3 Program.cs de l’AppHost (extrait)
```csharp
var builder = DistributedApplication.CreateBuilder(args);

var pg = builder.AddPostgres("agenda-db");

var connectionString = pg.GetConnectionString();

var migrator = builder.AddProject<Projects.Agenda_Migrator>("migrator")
    .WithReference(pg)
    .WithEnvironment("Provider", "postgres")
    .WithEnvironment("ConnectionStrings__Agenda", connectionString)
    .AsStartupTask();

var api = builder.AddProject<Projects.Agenda_API>("api")
    .WithReference(pg)
    .WithExternalHttpEndpoints()
    .WaitFor(migrator); // s’assurer que l’API attend la fin du migrator

builder.Build().Run();
```

### 7.4 Configuration dans Agenda.API (extrait)
- Retirer `services.AddAsyncInitializer<DataStoreMigrateInitializerAsync<AgendaDataStore>>`.
- Conserver l’usage de `configuration.GetConnectionString("Agenda")` — Aspire injectera `ConnectionStrings:Agenda`.

---

## 8. Notes et risques

- Si plusieurs DbContext apparaissent à l’avenir, faire évoluer Agenda.Migrator pour exécuter les migrations de chacun, dans l’ordre requis.
- Pour tests parallèles, envisager un schéma ou une base par exécution.
- Sur dev Windows sans Docker, envisager une option Provider=sqlite pour faciliter le run local des tests.

---

## 9. Validation

- `dotnet run -p Agenda.AppHost` doit démarrer Postgres, exécuter les migrations (Migrator exit 0), puis démarrer l’API; endpoint health/ready OK.
  - `./build.cmd unit-tests` (tests unitaires) démarre les tests unitaires et passent.
- `./build.cmd integration-tests` (tests d’intégration adaptés) démarre la stack test et passe.

---

## 10. Suivi des tâches

- [ ] Créer Agenda.ServiceDefaults et y déplacer les defaults communs
- [ ] Créer Agenda.Migrator et implémenter `MigrateAsync`
- [ ] Créer Agenda.AppHost et déclarer Postgres + Migrator + API + dépendances
- [ ] Nettoyer Agenda.API (reprendre la chaîne de connexion et retirer la migration au démarrage)
- [ ] Adapter les fixtures des tests d’intégration à DistributedApplicationFactory
- [ ] Vérifier exécution locale et CIs