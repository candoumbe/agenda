### Objectif
Migrer progressivement vos endpoints basés sur Ardalis.Endpoints vers FastEndpoints, tout en limitant le risque, en conservant la couverture de tests, et en améliorant la maintenabilité (validation, docs, auth, filtres/handlers, etc.).

---

### Vue d’ensemble des différences clés
- Ardalis.Endpoints
    - Modèle basé sur des classes qui héritent d’EndpointBase/EndpointBaseAsync.
    - Utilise l’infrastructure MVC (Controllers/Minimal APIs) pour le routing et les filtres.
    - Validation souvent via FluentValidation (ou ModelState), middleware perso.
- FastEndpoints
    - Modèle "feature-first" avec classes Endpoint<TRequest, TResponse> (ou Endpoint<TRequest>) et méthodes Configure/HandleAsync.
    - Routing, validation, auth/permissions, versioning, OpenAPI/Swagger, filtres (pré/post processors) intégrés.
    - S’intègre très bien avec Minimal APIs et Swagger via FastEndpoints.Swagger.

---

### Plan d’action par phases

#### Phase 0 — Préparation
1. Cartographier les endpoints existants
    - Lister: route, verbe HTTP, version(s), DTOs (request/response), validations, policies d’auth, rôles/permissions, codes d’erreur, conventions (ProblemDetails), filtres/middlewares spécifiques, pagination/sorting.
    - Identifier les endpoints critiques et les dépendances communes (services, UoW, repos, mapping).
2. Stabiliser la base de tests
    - S’assurer que les tests d’intégration actuels couvrent les chemins critiques (200/201, 400/404/409, 401/403, 422, 500).
    - Ajouter des tests manquants si nécessaire (particulièrement sur versioning et permissions).
3. Ajouter FastEndpoints sans perturber l’existant
    - NuGet: FastEndpoints, FastEndpoints.Swagger (facultatif), FastEndpoints.Security (si besoins JWT/Permissions).
    - Conserver Ardalis.Endpoints en parallèle pour une migration incrémentale.

#### Phase 1 — Mise en place de l’infrastructure FastEndpoints
1. Bootstrap dans Program.cs
    - services.AddFastEndpoints();
    - app.UseFastEndpoints();
    - Swagger: services.AddSwaggerDoc(); app.UseOpenApi(); app.UseSwaggerUi(); (selon vos besoins)
2. Convention de versioning et de routes
    - Décider: préfixe "/api" global, version dans le chemin (v1/v2) ou via en-têtes.
    - FastEndpoints supporte Endpoint.Version(int) et Endpoint.Routes("/v{version}/...").
3. Stratégie d’auth/autz
    - Mapper vos policies/roles existants vers: Roles("Admin"), Permissions(...), AuthSchemes(...), AllowAnonymous().
4. Validation
    - Conserver FluentValidation: ajouter validator par TRequest; FastEndpoints les exécute automatiquement.
    - Configurer la réponse d’erreur (ProblemDetails-like) si vous avez une convention.
5. Filtres/Logic transverse
    - Convertir vos filtres MVC/Middlewares ciblés en PreProcessors/PostProcessors FastEndpoints si plus pertinent (ex: corrélation, logging, métriques, UoW/transactions, soft-tenant).

#### Phase 2 — Migration endpoint par endpoint (incrémentale)
1. Sélectionner un endpoint peu risqué comme pilote (ex: GET simple)
2. Migrer la classe
    - Reprendre le DTO de requête/réponse tel quel si possible.
    - Créer une classe : Endpoint<TRequest, TResponse> (ou EndpointWithoutRequest si GET simple), implémenter Configure() et HandleAsync().
    - Déclarer: Verbs, Routes, Version, Summary, Auth/Permissions, Throttle/Caching si besoin.
3. Injection de dépendances
    - Constructor injection disponible. Sinon, utiliser Resolve<T>() dans HandleAsync si nécessaire.
    - Migrer les usages de HttpContext, CancellationToken (FastEndpoints passe ct en paramètre de HandleAsync).
4. Validation
    - Ajouter/brancher le Validator<TRequest> (FluentValidation). FastEndpoints retournera 400 avec détails de validation par défaut.
5. Réponses & erreurs
    - Utiliser SendAsync/SendOkAsync/SendCreatedAtAsync/SendNotFoundAsync/SendErrorsAsync.
    - Si vous avez une enveloppe ou ProblemDetails custom, centraliser via ResponseStarted/ExceptionMiddleware ou un Pre/PostProcessor.
6. Tests
    - Cloner le test d’intégration existant et le pointer sur la nouvelle route (ou la même, selon la migration). Vérifier statuts, payload, headers.
7. Déployer et monitorer
    - Vérifier logs, APM, métriques, temps de réponse. Comparer avec l’endpoint Ardalis jusqu’à confiance.

8. Répéter endpoint par endpoint
    - Migrer d’abord les endpoints stateless et sans transactions complexes.
    - Terminer par ceux avec transactions, streaming, long-polling, websockets, etc.

#### Phase 3 — Rétrocompatibilité et bascule
1. Compatibilité des routes
    - Si les chemins changent, offrir une redirection 301/308 ou conserver une route legacy marquée Obsolete pendant un cycle.
2. Contrats stables
    - Éviter de casser les DTO publics. Si nécessaire, versionner l’endpoint (v2) et maintenir v1 un temps.
3. Supprimer progressivement Ardalis.Endpoints
    - Une fois 100% migré et monitoré, retirer le package et les abstractions superflues.

---

### Mapping conceptuel (Ardalis → FastEndpoints)
- EndpointBaseAsync / EndpointBase
    - → Endpoint<TRequest, TResponse> ou EndpointWithoutRequest
- OnGet/OnPost/HandleAsync
    - → Configure() avec Verbs(Http.GET/POST/...) + HandleAsync(TRequest req, CancellationToken ct)
- Attributs [Authorize], [AllowAnonymous]
    - → Roles("..."), Permissions("..."), Policies("..."), AllowAnonymous() dans Configure
- ModelState + FluentValidation
    - → Validator<TRequest> (FluentValidation) exécuté automatiquement
- Filtres MVC (IActionFilter/IAsyncActionFilter)
    - → PreProcessor<TRequest>, PostProcessor<TRequest, TResponse>
- ProducesResponseType / Swagger atribs
    - → Summary(s => { s.Summary = "..."; s.Description = "..."; s.Responses[200] = "..."; }) + FastEndpoints.Swagger Examples
- RouteAttributes [HttpGet("/api/v1/items/{id}")]
    - → Routes("/api/v1/items/{id}").Version(1). Verbs(Http.GET)

---

### Exemples de code

#### Avant (Ardalis.Endpoints)
```csharp
public class GetAppointmentEndpoint : EndpointBaseAsync
    .WithRequest<GetAppointmentRequest>
    .WithActionResult<AppointmentDto>
{
    private readonly IAppointmentsService _svc;

    public GetAppointmentEndpoint(IAppointmentsService svc) => _svc = svc;

    [HttpGet("/api/v1/appointments/{id}")]
    [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public override async Task<ActionResult<AppointmentDto>> HandleAsync(
        GetAppointmentRequest req,
        CancellationToken ct = default)
    {
        var result = await _svc.FindAsync(req.Id, ct);
        if (result is null) return NotFound();
        return Ok(result);
    }
}
```

#### Après (FastEndpoints)
```csharp
public sealed class GetAppointmentEndpoint : Endpoint<GetAppointmentRequest, AppointmentDto>
{
    private readonly IAppointmentsService _svc;

    public GetAppointmentEndpoint(IAppointmentsService svc) => _svc = svc;

    public override void Configure()
    {
        Get("/api/v{version}/appointments/{id}");
        Version(1);
        // ou: Verbs(Http.GET); Routes("/api/v1/appointments/{id}");
        AllowAnonymous(); // ou Roles("Clinician");
        Summary(s =>
        {
            s.Summary = "Récupère un rendez-vous par identifiant";
            s.Description = "Retourne 404 si non trouvé";
            s.Response<AppointmentDto>(200, "OK");
        });
    }

    public override async Task HandleAsync(GetAppointmentRequest req, CancellationToken ct)
    {
        var dto = await _svc.FindAsync(req.Id, ct);
        if (dto is null)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        await SendOkAsync(dto, ct);
    }
}
```

#### Validator (inchangé si vous utilisez déjà FluentValidation)
```csharp
public sealed class GetAppointmentValidator : Validator<GetAppointmentRequest>
{
    public GetAppointmentValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
```

#### Program.cs (bootstrap minimal)
```csharp
var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

services.AddFastEndpoints();
services.AddSwaggerDoc(settings =>
{
    settings.Title = "Agenda API";
    settings.Version = "v1";
});

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.UseFastEndpoints(c =>
{
    c.Versioning.Prefix = "v"; // optionnel
});

app.UseOpenApi();
app.UseSwaggerUi(s => s.ConfigureDefaults());

app.Run();
```

---

### Points d’attention et bonnes pratiques
- Conventions d’erreurs
    - Si votre API répond en ProblemDetails (RFC7807), harmoniser via un ExceptionHandler global ou un PostProcessor qui enveloppe systématiquement les erreurs de validation/exception.
- Transactions/UoW
    - Envisager un PreProcessor/ScopedBehavior pour démarrer/commit/rollback une transaction autour d’un endpoint.
- Pagination/tri
    - Centraliser les conventions (query params: page, pageSize, sort) via un Request-DTO commun et un Validator partagé.
- Performance
    - PreProcessors pour la corrélation (trace-id), mise en cache en amont, validation légère avant hitting la DAL.
- Sécurité
    - Mapper vos Claims/Roles/Policies vers Roles/Permissions de FastEndpoints; tester 401/403 explicitement.
- Versioning
    - Préférer Version(int) et routes stables. Supporter au moins N-1 versions pendant la transition.
- Swagger/Docs
    - Utiliser Summary/Description/Example pour des docs à jour. Ajouter ExampleProviders si utile.
- Logging/Metrics
    - Injecter ILogger<TEndpoint> ou utiliser des Middlewares/PreProcessors dédiés (Serilog/OTel). Vérifier que les scopes et corrélations sont propagés.

---

### Stratégie de migration recommandée (pas-à-pas)
1. Intégrer FastEndpoints et Swagger dans l’appli, sans toucher aux endpoints actuels.
2. Migrer 1 à 3 endpoints GET simples. Valider perf, sécurité, docs, logs.
3. Migrer un POST/PUT avec validation complexe et transaction pour éprouver UoW/PreProcessors.
4. Migrer les endpoints protégés (JWT, roles). Vérifier 401/403/permission denied.
5. Migrer les endpoints versionnés. Vérifier side-by-side v1 et v2.
6. Mettre en place les conventions d’erreurs/apparence Swagger communes.
7. Basculer les routes legacy vers des redirections ou suppression progressive.
8. Nettoyage: retirer Ardalis.Endpoints et les artefacts obsolètes.

---

### Checklist de migration
- [ ] FastEndpoints et FastEndpoints.Swagger ajoutés, bootstrap OK.
- [ ] Conventions routes/versioning décidées et appliquées.
- [ ] Auth/Policies/Permissions mappées et testées.
- [ ] Validators migrés et erreurs 400 cohérentes.
- [ ] Pre/PostProcessors pour cross-cutting (logging, UoW, correlation) en place.
- [ ] Swagger: Summary/Responses/Examples présents et validés.
- [ ] Tests d’intégration mis à jour (statuts, payloads, headers, auth).
- [ ] Monitoring/metrics OK après chaque lot.
- [ ] Rétrocompatibilité/redirects gérés.
- [ ] Nettoyage des dépendances Ardalis une fois 100% migré.

---

### Aide à la décision rapide
- Petite base d’endpoints, peu de contraintes MVC: migration directe rapide.
- Nombreux filtres MVC/fonctions transverses: passer par Pre/PostProcessors, prévoir une phase d’adaptation.
- Contrats publics stables requis: versionner dans FastEndpoints et conserver legacy un temps.

---

### Besoin d’accompagnement spécifique
Si vous me partagez un endpoint Ardalis concret (classe + DTO + validator), je vous fournis sa conversion FastEndpoints prête à coller, y compris:
- Configure() complet (routes, verbes, auth, version, summary)
- HandleAsync avec logique, gestion d’erreurs, envoi de réponses
- Validator et exemples Swagger
- Adaptation des tests d’intégration correspondants