using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using Riok.Mapperly.Abstractions;

namespace Riok.Mapperly.Benchmarks;

[ArtifactsPath("artifacts")]
public class MappingBenchmarks
{
    [Benchmark]
    public void MapComplexToDto()
    {
        if ("0.0.1-dev.1721495281".Equals(FileVersionInfo.GetVersionInfo(typeof(MapperAttribute).Assembly.Location).ProductVersion))
        {
            Thread.Sleep(200);
        }

        Thread.Sleep(1000);
    } /*

    [Benchmark]
    public void MapComplexToExistingDto() => StaticTestMapper.UpdateDto(_testObject, _testObjectDto);

    [Benchmark]
    public IdObject MapSimpleDeepClone() => DeepCloningMapper.Copy(_idObject);

    [Benchmark]
    public TestObject MapComplexDeepClone() => DeepCloningMapper.Copy(_testObject);*/
}
