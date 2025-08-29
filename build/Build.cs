using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Candoumbe.Pipelines.Components;
using Candoumbe.Pipelines.Components.Formatting;
using Candoumbe.Pipelines.Components.GitHub;
using Candoumbe.Pipelines.Components.NuGet;
using Candoumbe.Pipelines.Components.Workflows;
using Nuke.Common;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.Codecov;
using Nuke.Common.Tools.Docker;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitHub;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Utilities;
using static Nuke.Common.Tools.Docker.DockerTasks;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Serilog.Log;

[GitHubActions(
                  "integration",
                  GitHubActionsImage.UbuntuLatest,
                  AutoGenerate = false,
                  FetchDepth = 0,
                  OnPushBranchesIgnore = [IHaveMainBranch.MainBranchName],
                  PublishArtifacts = true,
                  InvokedTargets = [nameof(Tests), nameof(IPushNugetPackages.Publish), nameof(IPack.Pack)],
                  CacheKeyFiles = ["global.json", "src/**/*.csproj"],
                  ImportSecrets =
                  [
                      nameof(IPushNugetPackages.NuGetApiKey),
                      nameof(IReportCoverage.CodecovToken),
                      nameof(IMutationTest.StrykerDashboardApiKey)
                  ],
                  OnPullRequestExcludePaths =
                  [
                      "docs/*",
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
                  OnPushBranches = [IHaveMainBranch.MainBranchName, IGitFlow.ReleaseBranch + "/*"],
                  InvokedTargets = [nameof(Tests), nameof(IPushNugetPackages.Publish), nameof(ICreateGithubRelease.AddGithubRelease)],
                  EnableGitHubToken = true,
                  CacheKeyFiles = ["global.json", "src/**/*.csproj"],
                  PublishArtifacts = true,
                  ImportSecrets =
                  [
                      nameof(IPushNugetPackages.NuGetApiKey),
                      nameof(IReportCoverage.CodecovToken),
                      nameof(IMutationTest.StrykerDashboardApiKey)
                  ],
                  OnPullRequestExcludePaths =
                  [
                      "docs/*",
                      "README.md",
                      "CHANGELOG.md",
                      "LICENSE"
                  ]
              )]
[DotNetVerbosityMapping]
public class Build : EnhancedNukeBuild,
    IHaveGitVersion,
    IHaveSourceDirectory,
    IHaveTestDirectory,
    IGitFlowWithPullRequest,
    IClean,
    IDotnetFormat,
    IRestore,
    IMutationTest,
    IBenchmark,
    IReportUnitTestCoverage,
    IReportIntegrationTestCoverage,
    IPushNugetPackages,
    ICreateGithubRelease
{

    [Solution] [Required] public readonly Solution Solution;

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

    ///<inheritdoc/>
    IEnumerable<Project> IUnitTest.UnitTestsProjects => this.Get<IHaveSolution>().Solution.GetAllProjects("*.UnitTests");

    ///<inheritdoc/>
    IEnumerable<Project> IIntegrationTest.IntegrationTestsProjects => this.Get<IHaveSolution>().Solution.GetAllProjects("*.IntegrationTests");

    private static readonly string[] s_projectWithUnitTests = ["Agenda.API", "Agenda.Ids", "Agenda.Objects"];

    ///<inheritdoc/>
    IEnumerable<MutationProjectConfiguration> IMutationTest.MutationTestsProjects
        => s_projectWithUnitTests
            .Select(projectName => new MutationProjectConfiguration(sourceProject: this.Get<IHaveSolution>().Solution.AllProjects.Single(csproj => string.Equals(csproj.Name, projectName, StringComparison.InvariantCultureIgnoreCase)),
                                                                    testProjects: this.Get<IHaveSolution>().Solution.AllProjects.Where(csproj => string.Equals(csproj.Name, $"{projectName}.UnitTests", StringComparison.InvariantCultureIgnoreCase)),
                                                                    configurationFile: this.Get<IHaveTestDirectory>().TestDirectory / $"{projectName}.UnitTests" / "stryker-config.json"))
            .ToArray();

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
                                         source: new Uri($"https://nukpg.github.com/{this.Get<IHaveGitHubRepository>().GitRepository.GetGitHubOwner()}/index.json"),
                                         () => this.Get<ICreateGithubRelease>()?.GitHubToken is not null)
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
    bool IDotnetFormat.VerifyNoChanges => IsServerBuild;

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
                                                                                      (setting, project) => setting.SetProjectFile(project)
                                                                                          .CombineWith(project.GetTargetFrameworks(),
                                                                                                       (x, framework) => x.SetFramework(framework)))
                                                                    )
                                                     );

    private AbsolutePath ApiProject => this.Get<IHaveSourceDirectory>().SourceDirectory / "Agenda.API";

    internal IReadOnlyList<RegistryConfiguration> Registries =>
    [
        new RegistryConfiguration("ghcr.io",
                                  this.Get<IHaveGitHubRepository>().GitRepository.GetGitHubOwner(),
                                  this.Get<IHaveGitHubRepository>().GitHubToken)
    ];

    public Target PublishApi => _ => _.Inherit<IPack>()
                                    .Description("Publish image of the API")
                                    .After(Tests)
                                    .Consumes(this.Get<ICompile>().Compile)
                                    .Produces(this.Get<IHaveArtifacts>().ArtifactsDirectory / "publish" / "*.tar.gz")
                                    .Executes(() =>
                                              {
                                                  GitVersion gitVersion = this.Get<IHaveGitVersion>().GitVersion;
                                                  string version = gitVersion.FullSemVer;
                                                  string imageName = "agenda.api";
                                                  string filename = $"{imageName}-{version}.tar.gz";
                                                  Project project = this.Get<IHaveSolution>().Solution.AllProjects.Single(project => project.Name == "Agenda.API");
                                                  AbsolutePath containerFullPath = this.Get<IHaveArtifacts>().ArtifactsDirectory / "publish" / filename;
                                                  Information("Publishing {ImageName} (version {Version}) to {ContainerFullPath}", project.Name, version, containerFullPath);

                                                  if (IsServerBuild)
                                                  {
                                                      DockerLogin(loginConfig => loginConfig.SetUsername(this.Get<IHaveGitRepository>().GitRepository.GetGitHubOwner())
                                                                      .SetPassword(this.Get<IHaveGitHubRepository>().GitHubToken));
                                                  }

                                                  DotNetPublish(settings => settings.SetProject(project)
                                                                    .SetConfiguration(this.Get<IHaveConfiguration>().Configuration)
                                                                    // .SetNoRestore(InvokedTargets.Contains(this.Get<IRestore>().Restore) && SucceededTargets.Contains(this.Get<IRestore>().Restore))
                                                                    // .SetNoBuild(InvokedTargets.Contains(this.Get<ICompile>().Compile) && SucceededTargets.Contains(this.Get<ICompile>().Compile))
                                                                    .EnableSelfContained()
                                                                    .When(IsServerBuild, target => target.SetProperty("ContainerRegistry", "ghcr.io"))
                                                                    .SetProperties(new Dictionary<string, object>
                                                                    {
                                                                        ["ContainerArchiveOutputPath"] = containerFullPath,
                                                                        ["ContainerImageName"] = imageName,
                                                                        ["ContainerImageTag"] = gitVersion.SemVer,
                                                                        //["PublishRepositoryUrl"] = true,
                                                                        ["ContainerGenerateLabelsImageCreated"] = DateTime.UtcNow.ToString("O")
                                                                    })
                                                                    .SetProcessAdditionalArguments([
                                                                        "/t:PublishContainer",
                                                                        "--tl"]));
                                              });


    public Target Tests => _ => _.Triggers(ArchitecturalTests,
                                           this.Get<IUnitTest>().UnitTests,
                                           this.Get<IIntegrationTest>().IntegrationTests)
                               .Description("Runs all tests");
}