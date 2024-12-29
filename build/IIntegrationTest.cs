using System;
using System.Collections.Generic;
using Candoumbe.Pipelines.Components;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.Coverlet;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.ReportGenerator;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Common.Tools.ReportGenerator.ReportGeneratorTasks;


public interface IIntegrationTest : IReportCoverage
{
    AbsolutePath IntegrationTestsResultDirectory => TestResultDirectory / "integration-tests";
    
    
    AbsolutePath CoverageReportIntegrationTestsDirectory => CoverageReportDirectory / "integration-tests";

    
    AbsolutePath CoverageReportIntegrationTestsHistoryDirectory => CoverageReportHistoryDirectory / "integration-tests";


    /// <summary>
    /// Integration test projects.
    /// </summary>
    IEnumerable<Project> Projects { get; }


    public Target IntegrationTests => _ => _
        .TryDependsOn<ICompile>()
        .TryBefore<IReportCoverage>()
        .Description("Runs integration tests.")
        .Produces(IntegrationTestsResultDirectory / "*.trx",
            IntegrationTestsResultDirectory / "*.xml",
            CoverageReportIntegrationTestsDirectory / "*.xml")
        .Executes(() =>
        {
            DotNetTest(s => s.Apply(IntegrationTestSettingsBase)
                .SetConfiguration(Configuration)
                .CombineWith(Projects,
                    (config, project) => config.SetProjectFile(project)
                        .CombineWith(project.GetTargetFrameworks(),
                            (setting, framework) => setting.SetFramework(framework)
                                .SetLoggers($"trx;LogFileName={project.Name}.{framework}.trx")
                                .SetCoverletOutput(IntegrationTestsResultDirectory / $"{project.Name}.xml")
                                .SetProcessEnvironmentVariable("DOTNET_URLS", "http://*:0;https://*:0"))
                )
            );

            if (ReportToCodeCov)
            {
                ReportGenerator(_ => _
                    .SetFramework("net5.0")
                    .SetReports(IntegrationTestsResultDirectory / "*.xml")
                    .SetReportTypes(ReportTypes.Badges, ReportTypes.HtmlChart, ReportTypes.HtmlInline)
                    .SetTargetDirectory(CoverageReportIntegrationTestsDirectory)
                    .SetHistoryDirectory(CoverageReportIntegrationTestsHistoryDirectory)
                );
            }
        });


    Configure<DotNetTestSettings> IntegrationTestSettingsBase => _ => _
        .EnableCollectCoverage()
        .WhenNotNull(this.As<ICompile>(), (settings, compile) => settings.EnableNoBuild())
        .SetResultsDirectory(IntegrationTestsResultDirectory)
        .SetCoverletOutputFormat(CoverletOutputFormat.cobertura)
        .AddProperty("ExcludeByAttribute", "Obsolete");


    /// <summary>
    /// Customize the behaviour of the component when running integration tests 
    /// </summary>
    Configure<DotNetTestSettings> IntegrationTestSettings => _ => _;
}