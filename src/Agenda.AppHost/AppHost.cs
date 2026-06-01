using Agenda.AppHost;
using Aspire.Hosting;
using Microsoft.Extensions.Configuration;
using Projects;

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
            .WithPgAdmin(containerName: "pg-admin")
            .WithPgWeb(containerName: "pg-web");
}

PinnedContainerImage rabbitImage = ContainerImages.RabbitMq;
var messaging = builder.AddRabbitMQ("messaging")
    .WithImage(rabbitImage.Image, rabbitImage.Tag)
    .WithManagementPlugin();

PinnedContainerImage keycloakImage = ContainerImages.Keycloak;
IResourceBuilder<KeycloakResource> keycloak = builder.AddKeycloak("keycloak")
    .WithImage(keycloakImage.Image, keycloakImage.Tag)
    .WithRealmImport("./keycloak/agenda-realm.json")
    .WithDeveloperCertificateTrust(trust: true)
    .WithExternalHttpEndpoints();

if (!isRunningIntegrationTests)
{
    keycloak = keycloak.WithDataVolume("keycloak-data");
}

IResourceBuilder<ProjectResource> migrationService = builder.AddProject<Agenda_Migrator>("migrations")
    .WithReference(postgres)
    .WaitFor(postgres);
    

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
     builder.AddJavaScriptApp("frontend", "../Agenda.Frontend", runScriptName)
              .WithDeveloperCertificateTrust(trust: true)
              .WithReference(api)
              .WaitFor(api)
              .WithReference(keycloak)
              .WaitFor(keycloak)
                // Demande à Aspire d’allouer un port et de le passer à l’app via la variable d’env PORT
              .WithHttpEndpoint(env: "PORT")
              .WithExternalHttpEndpoints()
          .PublishAsDockerFile();
}

builder.Build().Run();


public partial class Program
{
    public const string RunningIntegrationTestsConfigName = "RunningIntegrationTests";
}