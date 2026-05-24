using Aspire.Hosting;
using Microsoft.Extensions.Configuration;
using Projects;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithImage("postgres:17-alpine");


bool isRunningIntegrationTests = builder.Configuration.GetValue(RunningIntegrationTestsConfigName, false);

if (builder.ExecutionContext.IsRunMode && !isRunningIntegrationTests)
{
    postgres = postgres
            .WithPgAdmin(containerName: "pg-admin")
            .WithPgWeb(containerName: "pg-web");
}

var messaging = builder.AddRabbitMQ("messaging")
    .WithManagementPlugin();

IResourceBuilder<ProjectResource> migrationService = builder.AddProject<Agenda_Migrator>("migrations")
    .WithReference(postgres);

if (builder.ExecutionContext.IsRunMode)
{
    migrationService = migrationService.WaitFor(postgres);
}

IResourceBuilder<ProjectResource> api = builder.AddProject<Agenda_API>("api")
    .WithExternalHttpEndpoints()
    .WithReference(postgres)
    .WithReference(messaging);

if (builder.ExecutionContext.IsPublishMode)
{
    api = api.PublishAsDockerFile();
}

if (builder.ExecutionContext.IsRunMode)
{
    api = api
        .WaitFor(messaging)
        .WaitForCompletion(migrationService);
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