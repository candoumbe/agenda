using Microsoft.Extensions.Configuration;
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

var migrationService = builder.AddProject<Agenda_Migrator>("migrations")
    .WithReference(postgres)
    .WaitFor(postgres);

var api = builder.AddProject<Agenda_API>("api")
    //.WithReplicas(2)
    .WithHttpEndpoint(name: "unsecured")
    .WithHttpsEndpoint(name: "secured")
    .WithReference(postgres)
    .WaitForCompletion(migrationService)
    .PublishAsDockerFile();
builder.Build().Run();


public partial class Program
{
    public const string RunningIntegrationTestsConfigName = "RunningIntegrationTests";
}