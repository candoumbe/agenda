﻿using Candoumbe.Pipelines.Components;
using Candoumbe.Pipelines.Components.Docker;
using Candoumbe.Pipelines.Components.GitHub;
using Candoumbe.Pipelines.Components.NuGet;
using Candoumbe.Pipelines.Components.Workflows;

using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.Docker;

using System;
using System.Collections.Generic;
using System.Linq;

[GitHubActions(
    "integration",
    GitHubActionsImage.UbuntuLatest,
    FetchDepth = 0,
    OnPushBranchesIgnore = new[] { IHaveMainBranch.MainBranchName },
    PublishArtifacts = true,
    InvokedTargets = new[] { nameof(IUnitTest.UnitTests), nameof(IPushNugetPackages.Publish), nameof(IPack.Pack), nameof(IBuildDockerImage.BuildDockerImages) },
    CacheKeyFiles = new[] { "global.json", "src/**/*.csproj" },
    ImportSecrets = new[]
    {
            nameof(NugetApiKey),
            nameof(IReportCoverage.CodecovToken),
    },
    OnPullRequestExcludePaths = new[]
    {
            "docs/*",
            "README.md",
            "CHANGELOG.md",
            "LICENSE"
    }
)]
[GitHubActions(
    "delivery",
    GitHubActionsImage.UbuntuLatest,
    FetchDepth = 0,
    OnPushBranches = new[] { IHaveMainBranch.MainBranchName, IGitFlow.ReleaseBranch + "/*" },
    InvokedTargets = new[] { nameof(IUnitTest.UnitTests), nameof(IPushNugetPackages.Publish), nameof(ICreateGithubRelease.AddGithubRelease) },
    EnableGitHubToken = true,
    CacheKeyFiles = new[] { "global.json", "src/**/*.csproj" },
    PublishArtifacts = true,
    ImportSecrets = new[]
    {
            nameof(NugetApiKey),
            nameof(IReportCoverage.CodecovToken)
    },
    OnPullRequestExcludePaths = new[]
    {
            "docs/*",
            "README.md",
            "CHANGELOG.md",
            "LICENSE"
    }
)]

public class Build : NukeBuild,
    IHaveGitVersion,
    IHaveArtifacts,
    IHaveChangeLog,
    IHaveSolution,
    IHaveSourceDirectory,
    IHaveTestDirectory,
    IGitFlowWithPullRequest,
    IClean,
    IRestore,
    ICompile,
    IUnitTest,
    IMutationTest,
    IBenchmark,
    IReportCoverage,
    IPack,
    IPushNugetPackages,
    ICreateGithubRelease,
    IPushDockerImages
{
    [CI]
    public GitHubActions GitHubActions;

    [Parameter("API key used to publish artifacts to Nuget.org")]
    [Secret]
    public readonly string NugetApiKey;

    [Solution]
    [Required]
    public readonly Solution Solution;

    Solution IHaveSolution.Solution => Solution;

    ///<inheritdoc/>
    public static int Main() => Execute<Build>(x => ((ICompile)x).Compile);

    ///<inheritdoc/>
    IEnumerable<AbsolutePath> IClean.DirectoriesToDelete => this.Get<IHaveSourceDirectory>().SourceDirectory
                                                                .GlobDirectories("**/bin", "**/obj")
                                                                .Concat(this.Get<IHaveTestDirectory>().TestDirectory.GlobDirectories("**/bin", "**/obj"));

    ///<inheritdoc/>
    AbsolutePath IHaveSourceDirectory.SourceDirectory => RootDirectory / "src";

    ///<inheritdoc/>
    AbsolutePath IHaveTestDirectory.TestDirectory => RootDirectory / "tests";

    ///<inheritdoc/>
    IEnumerable<Project> IUnitTest.UnitTestsProjects => Solution.GetAllProjects("*UnitTests");

    ///<inheritdoc/>
    IEnumerable<(Project, IEnumerable<Project>)> IMutationTest.MutationTestsProjects => new[] {
        (Solution.GetProject("Agenda.API"), Solution.GetAllProjects("Agenda.API.UnitTests")),
        (Solution.GetProject("Agenda.Ids"), Solution.GetAllProjects("Agenda.Ids.UnitTests")),
        (Solution.GetProject("Agenda.Objects"), Solution.GetAllProjects("Agenda.Objects.UnitTests"))
    };

    ///<inheritdoc/>
    IEnumerable<Project> IBenchmark.BenchmarkProjects => Solution.GetAllProjects("*.PerfomanceTests");

    ///<inheritdoc/>
    bool IReportCoverage.ReportToCodeCov => this.Get<IReportCoverage>().CodecovToken is not null;

    ///<inheritdoc/>
    IEnumerable<AbsolutePath> IPack.PackableProjects => this.Get<IHaveSourceDirectory>().SourceDirectory
                                                            .GlobFiles("**/*.csproj", "!**/*.API.csproj");

    ///<inheritdoc/>
    IEnumerable<PushNugetPackageConfiguration> IPushNugetPackages.PublishConfigurations => new PushNugetPackageConfiguration[]
    {
            new NugetPushConfiguration   (apiKey: NugetApiKey,
                                          source: new Uri("https://api.nuget.org/v3/index.json"),
                                          () => NugetApiKey is not null),
            new GitHubPushNugetConfiguration(githubToken: this.Get<IHaveGitHubRepository>().GitHubToken,
                                           source: new Uri($"https://nukpg.github.com/{GitHubActions?.RepositoryOwner}/index.json"),
                                           () => this is ICreateGithubRelease && this.Get<ICreateGithubRelease>()?.GitHubToken is not null)
    };

    ///<inheritdoc/>
    IEnumerable<DockerFile> IBuildDockerImage.DockerFiles => new[]
    {
        new DockerFile(this.Get<IHaveSourceDirectory>().SourceDirectory / "Agenda.API" / "Dockerfile", "Agenda.API".ToLowerInvariant(), this.Get<IHaveGitVersion>().MajorMinorPatchVersion),
        new DockerFile(this.Get<IHaveSourceDirectory>().SourceDirectory / "Agenda.API" / "Dockerfile", "Agenda.API".ToLowerInvariant(), this.Get<IHaveGitRepository>().GitRepository.Branch switch {
                IHaveDevelopBranch.DevelopBranchName => "latest-alpha",
                IHaveMainBranch.MainBranchName => "latest",
                _ => $"{this.Get<IHaveGitVersion>().GitVersion.EscapedBranchName.ToLowerInvariant()}"
        })
    };

    ///<inheritdoc/>
    Configure<DockerBuildSettings> IBuildDockerImage.BuildSettings => _ => _.SetPath(".");

    ///<inheritdoc/>
    IEnumerable<string> IPushDockerImages.Images => this.Get<IBuildDockerImage>()
                                                        .DockerFiles
                                                        .Select(x => $"{x.Name}{(string.IsNullOrWhiteSpace(x.Tag) ? string.Empty : $":{x.Tag}")}");

    ///<inheritdoc/>
    IEnumerable<PushDockerImageConfiguration> IPushDockerImages.Registries => new[]
    {
        new PushDockerImageConfiguration(new Uri($"ghcr.io/{GitHubActions?.Repository}/"))
    };

    protected override void OnBuildCreated()
    {
        if (IsServerBuild)
        {
            EnvironmentInfo.SetVariable("DOTNET_ROLL_FORWARD", "LatestMajor");
        }
    }
}
