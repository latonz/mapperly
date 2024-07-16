using System.Linq;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;

namespace Riok.Mapperly.Benchmarks;

// TODO github CI
public class BenchmarkConfig : ManualConfig
{
    public static readonly IConfig Instance = new BenchmarkConfig();

    private BenchmarkConfig()
    {
        //AddJob(Job.Default.WithId("local").AsBaseline());
        AddJobForVersion("0.0.1-dev.1721173546");
        AddJobForVersion("0.0.1-dev.1721495281");
        AddColumnProvider(DefaultColumnProviders.Instance);
        HideColumns("Arguments", "NuGetReferences");
        AddLogger(ConsoleLogger.Default);
        AddExporter(HtmlExporter.Default);
    }

    private void AddJobForVersion(string version)
    {
        AddJob(
            Job.Default.WithId(version)
                .WithArguments([new MsBuildArgument($"-p:MapperlyNugetPackageVersion={version}")])
                .WithNuGet("Riok.Mapperly", version)
                .WithBaseline(!GetJobs().Any())
        );
    }
}
