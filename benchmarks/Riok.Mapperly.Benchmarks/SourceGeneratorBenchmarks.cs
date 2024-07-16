using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Loggers;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Text;
using Riok.Mapperly.Abstractions;

namespace Riok.Mapperly.Benchmarks;

[ArtifactsPath("artifacts")]
public class SourceGeneratorBenchmarks
{
    private const string SampleProjectPath = "../../../samples/Riok.Mapperly.Sample/Riok.Mapperly.Sample.csproj";
    private const string IntegrationTestProjectPath = "../../../test/Riok.Mapperly.IntegrationTests/Riok.Mapperly.IntegrationTests.csproj";
    private const string StaticTestMapperPath = "../../../test/Riok.Mapperly.IntegrationTests/Mapper/StaticTestMapper.cs";

    private MSBuildWorkspace? _workspace;
    private Project? _project;
    private CSharpParseOptions? _parseOptions => (CSharpParseOptions?)_project?.ParseOptions;

    private GeneratorDriver? _sampleDriver;
    private Compilation? _sampleCompilation;

    private GeneratorDriver? _largeDriver;
    private Compilation? _largeCompilation;

    private GeneratorDriver? _largeIncrementalDriver;
    private Compilation? _largeIncrementalCompilation;

    private string? _staticTestMapperPath;
    private SourceText? _modifiedStaticTestMapperSource;

    public SourceGeneratorBenchmarks()
    {
        try
        {
            MSBuildLocator.RegisterDefaults();
        }
        catch { }
    }

    private static string ResolveRelativePath(string path, [CallerFilePath] string callerFilePath = default!) =>
        Path.GetFullPath(Path.Combine(callerFilePath, path));

    private async Task<(Compilation, CSharpGeneratorDriver)> SetupAsync(string projectPath)
    {
        _workspace = MSBuildWorkspace.Create();
        _workspace.WorkspaceFailed += (_, args) =>
        {
            ConsoleLogger.Default.WriteLineError("-------------------------");
            ConsoleLogger.Default.WriteLineError(args.Diagnostic.ToString());
            ConsoleLogger.Default.WriteLineError("-------------------------");
        };

        var projectFile = ResolveRelativePath(projectPath);
        if (!File.Exists(projectFile))
            throw new Exception("Project doesn't exist");

        ConsoleLogger.Default.WriteLine($"Project exists at {projectFile}");

        try
        {
            ConsoleLogger.Default.WriteLine("Loading project\n");
            _project = await _workspace.OpenProjectAsync(projectFile);
            ConsoleLogger.Default.WriteLine("\nLoaded project");
        }
        catch (Exception ex)
        {
            ConsoleLogger.Default.WriteError(ex.Message);
            throw;
        }

        var compilation = await _project.GetCompilationAsync();
        if (compilation == null)
            throw new InvalidOperationException("Compilation returned null");

        var generator = new MapperGenerator().AsSourceGenerator();

        var driver = CSharpGeneratorDriver.Create([generator], parseOptions: _parseOptions!);

        return (compilation, driver);
    }

    [GlobalSetup(Target = nameof(Compile))]
    public void SetupCompile() => (_sampleCompilation, _sampleDriver) = SetupAsync(SampleProjectPath).GetAwaiter().GetResult();

    [GlobalSetup(Target = nameof(LargeCompile))]
    [MemberNotNull(nameof(_largeCompilation), nameof(_largeDriver))]
    public void SetupLargeCompile() => (_largeCompilation, _largeDriver) = SetupAsync(IntegrationTestProjectPath).GetAwaiter().GetResult();

    [GlobalSetup(Target = nameof(LargeIncrementalCompile))]
    public void SetupLargeIncrementalCompile()
    {
        SetupLargeCompile();

        _staticTestMapperPath = ResolveRelativePath(StaticTestMapperPath);
        var staticTestMapperSource = File.ReadAllText(_staticTestMapperPath);
        var modifiedStaticTestMapperSource = staticTestMapperSource.Replace(
            "[Mapper(EnumMappingStrategy = EnumMappingStrategy.ByValue)]",
            "[Mapper]",
            StringComparison.Ordinal
        );
        if (staticTestMapperSource.Equals(modifiedStaticTestMapperSource, StringComparison.Ordinal))
            throw new InvalidOperationException("Looks like the update on the static test mapper didn't work");

        _modifiedStaticTestMapperSource = SourceText.From(modifiedStaticTestMapperSource);

        _largeIncrementalDriver = _largeDriver.RunGeneratorsAndUpdateCompilation(
            _largeCompilation,
            out _largeIncrementalCompilation,
            out _
        );
    }

    [Benchmark]
    public void Compile()
    {
        _sampleDriver!.RunGeneratorsAndUpdateCompilation(_sampleCompilation!, out _, out _);
    }

    [Benchmark]
    public void LargeCompile() => _largeDriver!.RunGeneratorsAndUpdateCompilation(_largeCompilation!, out _, out _);

    [Benchmark]
    public void LargeIncrementalCompile()
    {
        var driver = _largeIncrementalDriver!.RunGeneratorsAndUpdateCompilation(_largeIncrementalCompilation!, out var compilation, out _);

        // rerun with added unrelated
        compilation = compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText("public record FooBarBenchmarkingType;", _parseOptions!));
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out compilation, out _);

        // rerun with modified mapper
        var staticTestMapperSource = compilation.SyntaxTrees.Single(x => x.FilePath.Equals(_staticTestMapperPath));
        var modifiedStaticTestMapperSource = staticTestMapperSource.WithChangedText(_modifiedStaticTestMapperSource!);
        compilation = compilation.ReplaceSyntaxTree(staticTestMapperSource, modifiedStaticTestMapperSource);
        driver.RunGeneratorsAndUpdateCompilation(compilation, out compilation, out _);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _workspace?.Dispose();
    }
}
