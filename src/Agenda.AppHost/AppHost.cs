using Agenda.AppHost;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel.Docker;
using Microsoft.Extensions.Configuration;
using Projects;

#pragma warning disable ASPIREDOCKERFILEBUILDER001

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

bool isRunningIntegrationTests = builder.Configuration.GetValue(RunningIntegrationTestsConfigName, false);
bool shouldTrackResourceHealth = !isRunningIntegrationTests;
string testingNow = builder.Configuration.GetValue<string>("Testing:Now");

PinnedContainerImage postgresImage = ContainerImages.Postgres;
var postgres = builder.AddPostgres("postgres")
    .WithImage(postgresImage.Image, postgresImage.Tag);

if (builder.ExecutionContext.IsRunMode && !isRunningIntegrationTests)
{
    postgres = postgres
            .WithDataVolume("postgres-data")
            .WithPgAdmin(containerName: "pg-admin",
                         configureContainer: pgAdmin => pgAdmin.WithImage(ContainerImages.PgAdmin.Image, ContainerImages.PgAdmin.Tag));
}

PinnedContainerImage rabbitImage = ContainerImages.RabbitMq;
var messaging = builder.AddRabbitMQ("messaging")
    .WithImage(rabbitImage.Image, rabbitImage.Tag)
    .WithManagementPlugin();


IResourceBuilder<ParameterResource> keycloakAdminUser = builder.AddParameter("keycloak-admin-user", secret: true);
IResourceBuilder<ParameterResource> keycloakAdminPassword = builder.AddParameter("keycloak-admin-password", secret: true);

PinnedContainerImage keycloakImage = ContainerImages.Keycloak;
IResourceBuilder<KeycloakResource> keycloak = builder.AddKeycloak("keycloak",
                                                                  adminUsername: keycloakAdminUser,
                                                                  adminPassword: keycloakAdminPassword)
    .WithImage(keycloakImage.Image, keycloakImage.Tag)
    .WithRealmImport("./keycloak/agenda-realm.json")
    .WithExternalHttpEndpoints();

if (!isRunningIntegrationTests)
{
    keycloak = keycloak.WithDataVolume("keycloak-data");
}

EndpointReference keycloakHttpEndpoint = keycloak.GetEndpoint("http");

IResourceBuilder<ProjectResource> migrationService = builder.AddProject<Agenda_Migrator>("migrations")
    .WithReference(postgres).WaitFor(postgres);
    

IResourceBuilder<ProjectResource> api = builder.AddProject<Agenda_API>("api")
    .WithHttpHealthCheck("/health", endpointName:"http")
    .WithExternalHttpEndpoints()
    .WithReference(postgres).WaitFor(postgres)
    .WithReference(messaging).WaitFor(messaging)
    .WithReference(keycloak).WaitFor(keycloak)
    .WaitForCompletion(migrationService);

if (!string.IsNullOrWhiteSpace(testingNow))
{
    api = api.WithEnvironment("Testing__Now", testingNow);
}

if (builder.ExecutionContext.IsPublishMode)
{
    api = api.PublishAsDockerFile();
}

if (!isRunningIntegrationTests)
{
    api = api.WithDeveloperCertificateTrust(trust: true);
}

string runScriptName = builder.ExecutionContext.IsRunMode ? "start:dev" : "start";

if (!isRunningIntegrationTests)
{
    var frontend = builder.AddViteApp("frontend", "../Agenda.Frontend", runScriptName)
              .WithDeveloperCertificateTrust(trust: true)
              .WithReference(api).WaitFor(api)
              .WithReference(keycloak).WaitFor(keycloak)
                // Ask Aspire to allocate a port and pass it to the app via the PORT environment variable
              .WithHttpEndpoint(env: "PORT")
              .WithExternalHttpEndpoints()
              .WithEnvironment("AGENDA_AUTH_AUTHORITY", $"{keycloakHttpEndpoint}/realms/agenda")
              .WithEnvironment("AGENDA_AUTH_CLIENT_ID", "agenda-frontend")
              .WithEnvironment("AGENDA_AUTH_SCOPE", "openid profile email agenda-audience")
              .PublishAsDockerFile(frontendApp =>
        {
#pragma warning disable ASPIREPIPELINES003 // Le type est utilisé à des fins d’évaluation uniquement et est susceptible d’être modifié ou supprimé dans les futures mises à jour. Supprimez ce diagnostic pour continuer.
            
#pragma warning restore ASPIREPIPELINES003 // Le type est utilisé à des fins d’évaluation uniquement et est susceptible d’être modifié ou supprimé dans les futures mises à jour. Supprimez ce diagnostic pour continuer.
        });

    if(builder.ExecutionContext.IsPublishMode)
    {
    }
}

builder.Build().Run();


public partial class Program
{
    public const string RunningIntegrationTestsConfigName = "RunningIntegrationTests";
}

#pragma warning restore ASPIREDOCKERFILEBUILDER001