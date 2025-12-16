using Aspire.Hosting.JavaScript;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Projects;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithImage("postgres:17-alpine");


bool isRunningIntegrationTests = builder.Configuration.GetValue(RunningIntegrationTestsConfigName, false);

if (! isRunningIntegrationTests)
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

builder.AddExecutable("frontend", "npm", "../Agenda.Frontend", args = ["run", "start"])
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints()
    .WithReference(api)
    .WaitFor(api)
    .PublishAsDockerFile();

builder.Build().Run();


public partial class Program
{
    public const string RunningIntegrationTestsConfigName = "RunningIntegrationTests";
}