using System;
using System.Collections.Generic;
using System.Linq;
using Agenda.AppHost;
using Candoumbe.Pipelines.Components;
using Candoumbe.Pipelines.Components.Formatting;
using Candoumbe.Pipelines.Components.GitHub;
using Candoumbe.Pipelines.Components.NuGet;
using Candoumbe.Pipelines.Components.Workflows;
using Fallout.Common;
using Fallout.Common.CI.GitHubActions;
using Fallout.Common.Git;
using Fallout.Common.IO;
using Fallout.Common.ProjectModel;
using Fallout.Common.Tooling;
using Fallout.Common.Tools.Codecov;
using Fallout.Common.Tools.Docker;
using Fallout.Common.Tools.DotNet;
using Fallout.Common.Tools.EntityFramework;
using Fallout.Common.Tools.GitHub;
using Fallout.Common.Tools.GitVersion;
using Fallout.Common.Tools.Npm;
using static Fallout.Common.Tools.Docker.DockerTasks;
using static Fallout.Common.Tools.DotNet.DotNetTasks;
using static Fallout.Common.Tools.EntityFramework.EntityFrameworkTasks;
using static Fallout.Common.Tools.Npm.NpmTasks;
using static Fallout.Common.Utilities.ConsoleUtility;
using static Serilog.Log;
using Project = Fallout.Common.ProjectModel.Project;

[GitHubActions(
    "integration",
    GitHubActionsImage.UbuntuLatest,
    AutoGenerate = false,
    FetchDepth = 0,
    OnPushBranchesIgnore = [IHaveMainBranch.MainBranchName],
    PublishArtifacts = true,
    EnableGitHubToken = true,
    InvokedTargets = [nameof(Tests), nameof(PublishImages), nameof(IPack.Pack)],
    CacheKeyFiles = ["global.json", "src/**/*.csproj"],
    ImportSecrets =
    [
        nameof(IPushNugetPackages.NuGetApiKey),
        nameof(IReportCoverage.CodecovToken),
        nameof(IMutationTest.StrykerDashboardApiKey)
    ],
    OnPushExcludePaths =
    [
        "docs/**",
        "README.md",
        "CHANGELOG.md",
        "LICENSE"
    ]
)]
[GitHubActions(
    "delivery",
    GitHubActionsImage.UbuntuLatest,
    FetchDepth = 0,
    AutoGenerate = false,
    OnPushBranches = [IHaveMainBranch.MainBranchName],
    InvokedTargets = [nameof(Tests), nameof(PublishImages), nameof(ICreateGithubRelease.AddGithubRelease)],
    EnableGitHubToken = true,
    CacheKeyFiles = ["global.json", "src/**/*.csproj"],
    PublishArtifacts = true,
    ImportSecrets =
    [
        nameof(IPushNugetPackages.NuGetApiKey),
        nameof(IReportCoverage.CodecovToken),
        nameof(IMutationTest.StrykerDashboardApiKey)
    ],
    OnPushExcludePaths =
    [
        "docs/**",
        "README.md",
        "CHANGELOG.md",
        "LICENSE"
    ]
)]
[DotNetVerbosityMapping]
public class Build : EnhancedBuild,
    IHaveGitVersion,
    IHaveSourceDirectory,
    IHaveTestDirectory,
    IGitFlowWithPullRequest,
    IDoChoreWorkflow,
    IClean,
    IRestore,
    IDotnetFormat,
    IBenchmark,
    IReportUnitTestCoverage,
    IReportIntegrationTestCoverage,
    IPushNugetPackages,
    ICreateGithubRelease,
    ICanRegenerateGitHubWorkflows
{

    [Solution][Required] public readonly Solution Solution;

    /// <inheritdoc />
    Solution IHaveSolution.Solution => Solution;

    public static int Main() => Execute<Build>(x => ((ICompile)x).Compile);

    ///<inheritdoc/>
    IEnumerable<AbsolutePath> IClean.DirectoriesToDelete =>
    [
        .. this.Get<IHaveSourceDirectory>().SourceDirectory.GlobDirectories("**/bin", "**/obj"),
        .. this.Get<IHaveTestDirectory>().TestDirectory.GlobDirectories("**/bin", "**/obj")
    ];

    ///<inheritdoc/>
    AbsolutePath IHaveSourceDirectory.SourceDirectory => RootDirectory / "src";

    ///<inheritdoc/>
    AbsolutePath IHaveTestDirectory.TestDirectory => RootDirectory / "tests";

    /// <summary>
    /// Path to the frontend directory.
    /// </summary>
    AbsolutePath FrontendDirectory => this.Get<IHaveSourceDirectory>().SourceDirectory / "Agenda.Frontend";

    ///<inheritdoc/>
    IEnumerable<Project> IUnitTest.UnitTestsProjects => this.Get<IHaveSolution>().Solution.GetAllProjects("*.UnitTests");

    ///<inheritdoc/>
    Configure<DotNetTestSettings, (Project project, string framework)> IUnitTest.ProjectUnitTestSettings => (settings, unitTestRunContext) => settings
        .ResetProjectFile()
        .ClearLoggers()
        .SetProcessAdditionalArguments($"--project {unitTestRunContext.project.Path}");

    ///<inheritdoc/>
    Configure<DotNetTestSettings, (Project project, string framework)> IIntegrationTest.ProjectIntegrationTestSettings => (settings, testRunContext) => settings
        .ResetProjectFile()
        .ClearLoggers()
        .SetProcessAdditionalArguments($"--project {testRunContext.project.Path}");

    ///<inheritdoc/>
    IEnumerable<Project> IIntegrationTest.IntegrationTestsProjects => this.Get<IHaveSolution>().Solution.GetAllProjects("*.IntegrationTests");

    ///<inheritdoc/>
    IEnumerable<Project> IBenchmark.BenchmarkProjects => this.Get<IHaveSolution>().Solution.GetAllProjects("*.PerformanceTests");

    ///<inheritdoc/>
    bool IReportCoverage.ReportToCodeCov => this.Get<IReportCoverage>().CodecovToken is not null;

    ///<inheritdoc/>
    IEnumerable<AbsolutePath> IPack.PackableProjects => this.Get<IHaveSourceDirectory>().SourceDirectory
        .GlobFiles("**/*.csproj", "!**/*.API.csproj");

    ///<inheritdoc/>
    IEnumerable<PushNugetPackageConfiguration> IPushNugetPackages.PublishConfigurations =>
    [
        new GitHubPushNugetConfiguration(githubToken: this.Get<IHaveGitHubRepository>().GitHubToken,
                                         source: new Uri($"https://nuget.pkg.github.com/{this.Get<IHaveGitHubRepository>().GitRepository.GetGitHubOwner()}/index.json"),
                                         canBeUsed:() => this.Get<ICreateGithubRelease>()?.GitHubToken is not null)
    ];

    /// <inheritdoc />
    Configure<CodecovSettings> IReportIntegrationTestCoverage.CodecovSettings => _ => _.SetFlags("integration-tests");

    /// <inheritdoc />
    Configure<CodecovSettings> IReportUnitTestCoverage.CodecovSettings => _ => _.SetFlags("unit-tests");

    /// <inheritdoc />
    string IReportIntegrationTestCoverage.CodeCoverageReportArtifactName => "integration-test-coverage-report";

    /// <inheritdoc />
    string IReportIntegrationTestCoverage.CodeCoverageHistoryReportArtifactName => "integration-test-coverage-history-report";

    /// <inheritdoc />
    string IReportUnitTestCoverage.CodeCoverageReportArtifactName => "unit-test-coverage-report";

    /// <inheritdoc />
    string IReportUnitTestCoverage.CodeCoverageHistoryReportArtifactName => "unit-test-coverage-history-report";

    protected override void OnBuildCreated()
    {
        if (IsServerBuild)
        {
            EnvironmentInfo.SetVariable("DOTNET_ROLL_FORWARD", "LatestMajor");
        }
    }

    /// <inheritdoc/>
    bool IDotnetFormat.VerifyNoChanges => IsLocalBuild;

    /// <inheritdoc />
    Configure<DotNetFormatSettings> IDotnetFormat.FormatSettings => _ => _
                                                                        .When(_ => IsLocalBuild,
                                                                            settings => settings.SetVerbosity(DotNetVerbosity.diagnostic));

    private IReadOnlyList<Project> ArchitecturalTestsProjects => [.. this.Get<IHaveSolution>().Solution.AllProjects.Where(project => project.Name.Like("*.ArchitecturalTests", ignoreCase: true))];

    /// <summary>
    /// Target to run architectural tests.
    /// </summary>
    public Target ArchitecturalTests => _ => _.TryTriggeredBy<IUnitTest>() // <- This will make architectural tests run whenever unit tests run
                                            .TryBefore<IMutationTest>()
                                            .TryDependsOn<ICompile>()
                                            .Description("Runs architectural tests")
                                            .Executes(() =>
                                                          DotNetTest(s => s.SetConfiguration(this.Get<IHaveConfiguration>().Configuration)
                                                                         .SetNoBuild(SucceededTargets.Contains(this.Get<ICompile>().Compile))
                                                                         .SetNoRestore(SucceededTargets.Contains(this.Get<IRestore>().Restore))
                                                                         .CombineWith(ArchitecturalTestsProjects,
                                                                                      (setting, project) => setting.SetProcessAdditionalArguments($"--project {project.Path}")
                                                                                          .CombineWith(project.GetTargetFrameworks(),
                                                                                                       (x, framework) => x.SetFramework(framework)))
                                                                    )
                                                     );

    private AbsolutePath ApiProject => this.Get<IHaveSourceDirectory>().SourceDirectory / "Agenda.API";

    internal IReadOnlyList<RegistryConfiguration> Registries =>
    [
        new RegistryConfiguration("GitHub Container Registry",
                                  "ghcr.io",
                                  this.Get<IHaveGitHubRepository>().GitRepository.GetGitHubOwner(),
                                  this.Get<IHaveGitHubRepository>().GitHubToken)
    ];


    public Target RestoreFrontend => _ => _.Description("Restore frontend dependencies")
        .TryTriggeredBy<IRestore>()
        .Before(BuildFrontend)
        .Executes(() => NpmInstall(settings => settings.SetProcessWorkingDirectory(FrontendDirectory)));

    public Target BuildFrontend => _ => _.Description("Builds the frontend")
        .TryTriggeredBy<ICompile>()
        .DependsOn(RestoreFrontend)
        .Executes(() =>
        {
            NpmRun(settings => settings.SetProcessWorkingDirectory(FrontendDirectory)
                                                                   .SetCommand("build"));
        });

    public Target TestFrontend => _ => _.Description("Run frontend tests")
        .TryTriggeredBy<IUnitTest>()
        .DependsOn(BuildFrontend)
        .TryBefore<IReportUnitTestCoverage>()
        .Executes(() => NpmRun(settings => settings.SetProcessWorkingDirectory(FrontendDirectory)
                                                                         .SetProcessAdditionalArguments("--watch false")
                                                                         .SetCommand("test")));

    /// <summary>
    /// Pre-pulls every container image declared in <see cref="ContainerImages"/> so the download
    /// cost is paid before the integration test fixture starts the AppHost. This keeps cold CI
    /// runs from exhausting the AppHost startup timeout while pulling Postgres/RabbitMQ/Keycloak.
    /// </summary>
    public Target PrePullImages => _ => _
        .Description("Pulls Docker images required by integration tests so the pull time does not count against the AppHost startup timeout.")
        .TryDependentFor<IIntegrationTest>()
        .Executes(() =>
        {
            string[] references = [.. ContainerImages.All.Values.Select(image => image.FullReference)];
            DockerPull(settings => settings
                           .CombineWith(references,
                                        (s, image) => s.SetName(image)),
                       degreeOfParallelism: references.Length,
                       completeOnFailure: false);
        });


    public Target PublishApi => _ => _.Description("Publish image of the API")
            .DependsOn<IPushNugetPackages>()
            .TriggeredBy<IPushNugetPackages>()
            .After(Tests)
            .TryAfter<IPack>()
            .Consumes(this.Get<ICompile>().Compile)
            .Produces(this.Get<IHaveArtifacts>().ArtifactsDirectory / "publish" / "**" / "*.tar.gz")
            .Executes(() =>
            {
                GitVersion gitVersion = this.Get<IHaveGitVersion>().GitVersion;
                string version = gitVersion.FullSemVer;
                const string imageName = "agenda.api";

                string filename = $"{imageName}-{version}.tar.gz";
                Project project = this.Get<IHaveSolution>().Solution.AllProjects.Single(project => project.Name == "Agenda.API");

                Registries.ForEach(registry =>
                {
                    AbsolutePath containerFullPath = this.Get<IHaveArtifacts>().ArtifactsDirectory / "publish" / registry.Name / filename;

                    Information("Publishing {ImageName} (version {Version}) for {RegistryName} ({RegistryUri}) to {ContainerFullPath}",
                        project.Name, version, registry.Name, registry.Uri, containerFullPath);

                    string imageNameWithRegistry = $"{registry.Uri}/{this.Get<IHaveGitRepository>().GitRepository.GetGitHubOwner()}/{imageName}";
                    IDictionary<string, object> publishProperties = new Dictionary<string, object>
                    {
                        ["ContainerArchiveOutputPath"] = containerFullPath,
                        ["ContainerRepository"] = imageNameWithRegistry,
                        ["ContainerImageTag"] = gitVersion.SemVer,
                        ["ContainerImageFormat"] = "Docker",
                        ["ContainerRuntime"] = "docker",
                        ["ContainerGenerateLabelsImageCreated"] = DateTime.UtcNow.ToString("O")
                    };

                    DotNetPublish(settings => settings.SetProject(project)
                        .SetConfiguration(this.Get<IHaveConfiguration>().Configuration)
                        .SetProperties(publishProperties)
                        .SetProcessAdditionalArguments(["/t:PublishContainer", "--tl"]));

                    Information("{ImageName} (version {Version}) published successfully to {ContainerFullPath}", project.Name, version, containerFullPath);

                    Verbose("Loading image {ImageName} from archive {ContainerFullPath} ", imageNameWithRegistry, containerFullPath);
                    DockerLoad(settings => settings.SetInput(containerFullPath));

                    Verbose("Image {ImageName} loaded successfully", imageNameWithRegistry);

                    IReadOnlySet<string> tags = GenerateDockerTagsForBranch(this.Get<IHaveGitHubRepository>().GitRepository, gitVersion);
                    Verbose("Tagging image {ImageName} with tags: {@Tags}", imageNameWithRegistry, tags);

                    DockerImageTag(settings => settings.SetSourceImage($"{imageNameWithRegistry}:{gitVersion.SemVer}")
                        .CombineWith(tags, (dockerTagSettings, tag) => dockerTagSettings.SetTargetImage($"{imageNameWithRegistry}:{tag}")));

                    Verbose("Image {ImageName} tagged successfully", imageNameWithRegistry);

                    if (IsServerBuild)
                    {
                        Information("Pushing image {ImageName} to {RegistryName} ({RegistryUri}) with tags: {@Tags}",
                            imageNameWithRegistry, registry.Name, registry.Uri, tags);

                        Verbose("Logging into {RegistryUri}", registry.Uri);

                        DockerLogin(settings => settings.SetUsername(this.Get<IHaveGitHubRepository>().GitRepository.GetGitHubOwner())
                            .SetPassword(registry.Password)
                            .SetServer(registry.Uri));

                        Verbose("Logged into {RegistryUri} successfully", registry.Uri);

                        DockerImagePush(settings =>
                            settings.CombineWith(tags, (pushSettings, tag) => pushSettings.SetName($"{imageNameWithRegistry}:{tag}")));

                        Information("Image {ImageName} pushed successfully", imageNameWithRegistry);
                    }
                });
            });

    public Target PublishWorker => _ => _.Description("Publish image of the migration worker")
        .After(Tests)
        .Consumes(this.Get<ICompile>().Compile)
        .Produces(this.Get<IHaveArtifacts>().ArtifactsDirectory / "publish" / "**" / "*.tar.gz")
        .Executes(() =>
        {
            GitVersion gitVersion = this.Get<IHaveGitVersion>().GitVersion;
            string version = gitVersion.FullSemVer;
            const string imageName = "agenda.worker";

            string filename = $"{imageName}-{version}.tar.gz";
            Project project = this.Get<IHaveSolution>().Solution.AllProjects.Single(project => project.Name == "Agenda.Migrator");

            Registries.ForEach(registry =>
            {
                AbsolutePath containerFullPath = this.Get<IHaveArtifacts>().ArtifactsDirectory / "publish" / registry.Name / filename;

                Information("Publishing {ImageName} (version {Version}) for {RegistryName} ({RegistryUri}) to {ContainerFullPath}",
                    project.Name, version, registry.Name, registry.Uri, containerFullPath);

                string imageNameWithRegistry = $"{registry.Uri}/{this.Get<IHaveGitRepository>().GitRepository.GetGitHubOwner()}/{imageName}";
                IDictionary<string, object> publishProperties = new Dictionary<string, object>
                {
                    ["ContainerArchiveOutputPath"] = containerFullPath,
                    ["ContainerRepository"] = imageNameWithRegistry,
                    ["ContainerImageTag"] = gitVersion.SemVer,
                    ["ContainerImageFormat"] = "Docker",
                    ["ContainerRuntime"] = "docker",
                    ["ContainerGenerateLabelsImageCreated"] = DateTime.UtcNow.ToString("O")
                };

                DotNetPublish(settings => settings.SetProject(project)
                    .SetConfiguration(this.Get<IHaveConfiguration>().Configuration)
                    .EnableSelfContained()
                    .SetProperties(publishProperties)
                    .SetProcessAdditionalArguments(["/t:PublishContainer", "--tl"]));

                Information("{ImageName} (version {Version} published successfully to {ContainerFullPath}", project.Name, version, containerFullPath);

                Verbose("Loading image {ImageName} from {ContainerFullPath}", imageNameWithRegistry, containerFullPath);
                DockerLoad(settings => settings.SetInput(containerFullPath));

                Verbose("Image {ImageName} loaded successfully", imageNameWithRegistry);

                IReadOnlySet<string> tags = GenerateDockerTagsForBranch(this.Get<IHaveGitHubRepository>().GitRepository, gitVersion);
                Verbose("Tagging image {ImageName} with tags: {@Tags}", imageNameWithRegistry, tags);

                DockerImageTag(settings => settings.SetSourceImage($"{imageNameWithRegistry}:{gitVersion.SemVer}")
                    .CombineWith(tags, (dockerTagSettings, tag) => dockerTagSettings.SetTargetImage($"{imageNameWithRegistry}:{tag}")));
                Verbose("Image {ImageName} tagged successfully", imageNameWithRegistry);

                if (IsServerBuild)
                {
                    Information("Pushing image {ImageName} to {RegistryName} ({RegistryUri}) with tags: {@Tags}",
                        imageNameWithRegistry, registry.Name, registry.Uri, tags);

                    Verbose("Logging into {RegistryUri}", registry.Uri);

                    DockerLogin(settings => settings.SetUsername(this.Get<IHaveGitHubRepository>().GitRepository.GetGitHubOwner())
                        .SetPassword(registry.Password)
                        .SetServer(registry.Uri));

                    Verbose("Logged into {RegistryUri} successfully", registry.Uri);

                    DockerImagePush(settings =>
                        settings.CombineWith(tags, (pushSettings, tag) => pushSettings.SetName($"{imageNameWithRegistry}:{tag}")));

                    Information("Image {ImageName} pushed successfully", imageNameWithRegistry);
                }
            });
        });


    private static IReadOnlySet<string> GenerateDockerTagsForBranch(GitRepository repository, GitVersion version)
    {
        HashSet<string> tags = new(StringComparer.OrdinalIgnoreCase);

        if (repository.IsOnReleaseBranch())
        {
            tags.Add($"{version.Major}.{version.Minor}{version.PreReleaseLabelWithDash}");
            tags.Add($"{version.MajorMinorPatch}{version.PreReleaseLabelWithDash}");
        }
        else if (repository.IsOnHotfixBranch()
                 || repository.IsOnFeatureBranch()
                 || (repository.Branch?.Like("chore/*", ignoreCase: true) ?? false)
                 || (repository.Branch?.Like("coldfix/*", ignoreCase: true) ?? false))
        {
            tags.Add($"{version.Major}{version.PreReleaseLabelWithDash}");
            tags.Add($"{version.Major}.{version.Minor}{version.PreReleaseLabelWithDash}");
            tags.Add($"{version.Major}.{version.Minor}{version.PreReleaseLabelWithDash}.{version.ShortSha}");
            tags.Add($"{version.MajorMinorPatch}{version.PreReleaseLabelWithDash}");
            tags.Add($"{version.MajorMinorPatch}{version.PreReleaseLabelWithDash}.{version.ShortSha}");
        }
        else if (repository.IsOnDevelopBranch())
        {
            tags.Add($"{version.Major}-{version.EscapedBranchName}");
            tags.Add($"{version.Major}{version.PreReleaseLabelWithDash}");
            tags.Add($"{version.Major}.{version.Minor}{version.PreReleaseLabelWithDash}");
            tags.Add($"{version.Major}.{version.Minor}-{version.EscapedBranchName}");
            tags.Add($"{version.Major}.{version.Minor}-{version.EscapedBranchName}.{version.ShortSha}");
            tags.Add($"{version.MajorMinorPatch}{version.PreReleaseLabelWithDash}");
        }
        else if (repository.IsOnMainOrMasterBranch())
        {
            tags.Add($"{version.Major}");
            tags.Add($"{version.Major}-latest");
            tags.Add($"{version.Major}.{version.Minor}");
            tags.Add($"{version.Major}.{version.Minor}-latest");
            tags.Add($"{version.MajorMinorPatch}");
        }

        return tags;
    }

    public Target PublishFrontend => _ => _.Description("Publish frontend static files")
        .DependsOn(TestFrontend)
        .After(TestFrontend)
        .Consumes(BuildFrontend)
        .TryAfter<IPack>()
        .Produces(this.Get<IHaveArtifacts>().ArtifactsDirectory / "publish" / "frontend" / "**" / "*.tar.gz")
        .Executes(() =>
        {
            const string imageName = "agenda.frontend";
            GitVersion gitVersion = this.Get<IHaveGitVersion>().GitVersion;
            IReadOnlySet<string> versions = GenerateDockerTagsForBranch(this.Get<IHaveGitRepository>().GitRepository, gitVersion);

            string version = gitVersion.SemVer;

            DockerBuild(settings => settings.SetFile(FrontendDirectory / "Dockerfile")
                    .SetPath(FrontendDirectory)
                    .SetProcessWorkingDirectory(FrontendDirectory)
                    .SetTag([$"{imageName}:{version}", .. versions.Select(version => $"{imageName}:{version}")]));

            Information("{ImageName} (version {Version}) will be tagged as {Tag}", imageName, gitVersion.FullSemVer, version);

            Registries.ForEach(registry =>
            {
                string imageNameWithRegistry = $"{registry.Uri}/{this.Get<IHaveGitRepository>().GitRepository.GetGitHubOwner()}/{imageName}";

                Information("Publishing {ImageName} (version {Version}) to {RegistryName} ({RegistryUri}) as {ImageNameWithRegistry}",
                    imageName, version, registry.Name, registry.Uri, imageNameWithRegistry);


                Information("{ImageName} (version {Version}) loaded successfully", imageName, version);

                Verbose("Tagging image {ImageName} with tags: {@Tags}", imageNameWithRegistry, versions);

                DockerImageTag(settings => settings.SetSourceImage($"{imageName}:{version}")
                    .CombineWith(versions, (dockerTagSettings, tag) => dockerTagSettings.SetTargetImage($"{imageNameWithRegistry}:{tag}")));

                Verbose("Image {ImageName} tagged successfully", imageNameWithRegistry);

                if (IsServerBuild)
                {
                    Information("Pushing image {ImageName} to {RegistryName} ({RegistryUri}) with tags: {@Tags}",
                        imageNameWithRegistry, registry.Name, registry.Uri, versions);

                    Verbose("Logging into {RegistryUri}", registry.Uri);

                    DockerLogin(settings => settings.SetUsername(this.Get<IHaveGitHubRepository>().GitRepository.GetGitHubOwner())
                        .SetPassword(registry.Password)
                        .SetServer(registry.Uri));

                    Verbose("Logged into {RegistryUri} successfully", registry.Uri);

                    DockerImagePush(settings =>
                        settings
                            .CombineWith(versions, (pushSettings, tag) => pushSettings.SetName($"{imageNameWithRegistry}:{tag}")));

                    Information("Image {ImageName} pushed successfully", imageNameWithRegistry);
                }
            });
        });


    public Target Tests => _ => _.Triggers(ArchitecturalTests,
                                           this.Get<IUnitTest>().UnitTests,
                                           this.Get<IIntegrationTest>().IntegrationTests)
                               .Description("Runs all tests");

    public Target AddMigration => _ => _.Description("Add a new migration to the database")
        .OnlyWhenStatic(() => IsLocalBuild)
        .Executes(() =>
        {


            string migrationName = PromptForInput("New migration name (leave empty to cancel the operation): ", string.Empty);
            if (string.IsNullOrWhiteSpace(migrationName))
            {
                return;
            }
            string provider = PromptForChoice("Database provider : ", [("Postgres", "Postgres database engine"), ("Sqlite", "SQLite database engine")]);
            if (string.IsNullOrWhiteSpace(provider))
            {
                return;
            }

            const string migrationDirectoryName = "Migrations";
            const string contextName = "Agenda.DataStores.AgendaDataStore";

            if (PromptForChoice($"Adding migration '{migrationName}' for provider '{provider}'. Confirm ?",
                   [ (ConsoleKey.Y, "Confirm the operation"),
                       (ConsoleKey.N, "Cancel the operation")]) == ConsoleKey.N)
            {
                Information("Operation cancelled by the user.");
                return;
            }

            string connectionString = provider switch
            {
                "Postgres" => "Host=localhost;Port=5432;Database=agenda;Username=postgres;Password=!",
                "Sqlite" => "Data Source=agenda.db",
                _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, "Unsupported provider")
            };

            EntityFrameworkMigrationsAdd(s => s.SetStartupProject(ApiProject)
                .SetName(migrationName)
                .SetContext(contextName)
                .SetFramework("net10.0")
                .SetProject(this.Get<IHaveSourceDirectory>().SourceDirectory / $"Agenda.DataStores.{provider}" / $"Agenda.DataStores.{provider}.csproj")
                .SetStartupProject(ApiProject)
                .SetOutputDirectory(migrationDirectoryName)
                .SetProcessAdditionalArguments($"""
                                                -- --provider {provider.ToLowerInvariant()} --ConnectionStrings:agenda "{connectionString}"
                                                """));

            Information("Migration '{MigrationName}' added successfully.", migrationName);


        });

    public Target RemoveMigration => _ => _.Description("Remove latest migration")
        .OnlyWhenStatic(() => IsLocalBuild)
        .Executes(() =>
        {
            string provider = PromptForChoice("Database provider : ",
                                              [("Postgres", "Postgres database engine"), ("Sqlite", "SQLite database engine")]);
            if (string.IsNullOrWhiteSpace(provider))
            {
                return;
            }

            const string contextName = "Agenda.DataStores.AgendaDataStore";

            if (PromptForChoice($"Removing latest migration for provider '{provider}'. Confirm ?",
                   [ (ConsoleKey.Y, "Confirm the operation"),
                       (ConsoleKey.N, "Cancel the operation")]) == ConsoleKey.N)
            {
                Information("Operation cancelled by the user.");
                return;
            }

            string connectionString = provider switch
            {
                "Postgres" => "Host=localhost;Port=5432;Database=agenda;Username=postgres;Password=!",
                "Sqlite" => "Data Source=agenda.db",
                _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, "Unsupported provider")
            };

            EntityFrameworkMigrationsRemove(s => s.SetStartupProject(ApiProject)
                .SetContext(contextName)
                .SetProject(this.Get<IHaveSourceDirectory>().SourceDirectory / $"Agenda.DataStores.{provider}" / $"Agenda.DataStores.{provider}.csproj")
                .SetStartupProject(ApiProject)
                .SetProcessAdditionalArguments($"""
                                                -- --provider {provider.ToLowerInvariant()} --ConnectionStrings:agenda "{connectionString}"
                                                """));

            Information("Latest migration removed successfully.");
        });


    public Target PublishImages => _ => _.Description("Publish images of the API and frontend")
        .DependsOn(PublishApi, PublishFrontend, PublishWorker)
        .After(Tests)
        .TryBefore<ICreateGithubRelease>(x => x.AddGithubRelease)
        .Consumes(this.Get<ICompile>().Compile);

    public Target CleanImages => _ => _.OnlyWhenStatic(() => IsLocalBuild)
        .Description("Cleans up all images of the API and frontend")
        .Executes(async () =>
        {
            Information("Select repository where you want to clean up images:");
            string repository = PromptForChoice("Repository : ", Registries.Select(r => (r.Uri, $"{r.Name} ({r.Uri})")).ToArray());

            if (string.IsNullOrWhiteSpace(repository))
            {
                Information("Operation cancelled by the user.");
                return;
            }

            RegistryConfiguration registry = Registries.Single(r => r.Uri == repository);
            switch (PromptForChoice($"Are you sure you want to clean up all images of the API and frontend from {registry.Name} ({registry.Uri}) ?",
                   [ (ConsoleKey.Y, "Confirm the operation"),
                       (ConsoleKey.N, "Cancel the operation")]))
            {
                case ConsoleKey.Y:
                    Information("Cleaning up images from {RegistryName} ({RegistryUri})", registry.Name, registry.Uri);

                    if (repository.Like("ghcr.io", ignoreCase: true))
                    {
                        string owner = this.Get<IHaveGitHubRepository>().GitRepository.GetGitHubOwner();
                        string[] images = ["agenda.api", "agenda.frontend", "agenda.worker"];
                        // Choose which image to delete
                        string imageToDelete = PromptForChoice("Select image to delete: ", images.Select(image => (image, image)).ToArray());
                        if (string.IsNullOrWhiteSpace(imageToDelete))
                        {
                            Information("Operation cancelled by the user.");
                            return;
                        }

                        Information("Deleting image {ImageName} from {RegistryName} ({RegistryUri})", imageToDelete, registry.Name, registry.Uri);

                        // Choose which tag to delete
                        string tagToDelete = PromptForInput($"Enter the tag to delete for image {imageToDelete} (leave empty to cancel the operation): ", string.Empty);
                        if (string.IsNullOrWhiteSpace(tagToDelete))
                        {
                            Information("Operation cancelled by the user.");
                            return;
                        }

                        Information("Deleting tag {Tag} for image {ImageName} from {RegistryName} ({RegistryUri})", tagToDelete, imageToDelete, registry.Name, registry.Uri);
                        // Delete the image tag using GitHub API
                        Octokit.GitHubClient client = new(new Octokit.ProductHeaderValue("Agenda.Pipelines"))
                        {
                            Credentials = new Octokit.Credentials(this.Get<IHaveGitHubRepository>().GitHubToken)
                        };
                        Octokit.Package package = await client.Packages.GetForUser(owner, Octokit.PackageType.Container, imageToDelete);
                        if (package is null)
                        {
                            Information("Image {ImageName} not found in {RegistryName} ({RegistryUri})", imageToDelete, registry.Name, registry.Uri);
                            return;
                        }

                        Octokit.ApiOptions options = new()
                        { PageSize = 100 };
                        int page = 1;
                        List<Octokit.PackageVersion> allVersions = new(capacity: 300);
                        IReadOnlyList<Octokit.PackageVersion> pageOfVersions = Array.Empty<Octokit.PackageVersion>();
                        do
                        {
                            options.StartPage = page;
                            pageOfVersions = await client.Packages.PackageVersions.GetAllForUser(owner, Octokit.PackageType.Container, imageToDelete, options: options);
                            allVersions.AddRange(pageOfVersions);
                            page++;
                        } while (pageOfVersions.Count == 100);

                        Octokit.PackageVersion versionToDelete = allVersions.SingleOrDefault(v => v.Metadata.Container.Tags.Contains(tagToDelete));
                        if (versionToDelete is null)
                        {
                            Information("Tag {Tag} for image {ImageName} not found in {RegistryName} ({RegistryUri})", tagToDelete, imageToDelete, registry.Name, registry.Uri);
                        }
                        else
                        {
                            await client.Packages.PackageVersions.DeleteForUser(owner, Octokit.PackageType.Container, imageToDelete, Convert.ToInt32(versionToDelete.Id));
                            Information("Tag {Tag} for image {ImageName} deleted successfully from {RegistryName} ({RegistryUri})", tagToDelete, imageToDelete, registry.Name, registry.Uri);
                        }

                    }
                    else
                    {
                        Information("Cleaning up images from {RegistryName} ({RegistryUri}) is not supported yet.", registry.Name, registry.Uri);
                    }

                    break;
                case ConsoleKey.N:
                    Information("Operation cancelled by the user.");
                    break;
            }


        });
}