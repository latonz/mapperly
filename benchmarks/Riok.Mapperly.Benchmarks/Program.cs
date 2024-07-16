using BenchmarkDotNet.Running;
using Riok.Mapperly.Benchmarks;

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, BenchmarkConfig.Instance);
