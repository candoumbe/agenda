using Aspire.Hosting;
using Projects;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin(containerName: "pg-admin")
    .WithPgWeb(containerName: "pg-web")
    .WithDataVolume(name: "postgres-data");

var migrationService = builder.AddProject<Agenda_Migrator>("migrations")
    .WithReference(postgres)
    .WaitFor(postgres);

var api = builder.AddProject<Agenda_API>("api")
    //.WithReplicas(2)
    .WithHttpEndpoint(name: "unsecured")
    .WithHttpsEndpoint(name: "secured")
    .WithReference(postgres)
    .WaitForCompletion(migrationService)
    .WaitFor(postgres)
    .PublishAsDockerFile();

builder.Build().Run();


public partial class Program;