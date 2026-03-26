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
            .WithDataVolume(name: "postgres-data")
            .WithPgAdmin(containerName: "pg-admin")
            .WithPgWeb(containerName: "pg-web");
}

var messaging = builder.AddRabbitMQ("messaging")
    .WithManagementPlugin();

var migrationService = builder.AddProject<Agenda_Migrator>("migrations")
    .WithReference(postgres)
    .WaitFor(postgres);

var api = builder.AddProject<Agenda_API>("api")
    .WithDeveloperCertificateTrust(trust: true)
    .WithExternalHttpEndpoints()
    .WithReference(postgres)
    .WithReference(messaging)
    .WaitFor(messaging)
    .WaitForCompletion(migrationService)
    .PublishAsDockerFile();

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