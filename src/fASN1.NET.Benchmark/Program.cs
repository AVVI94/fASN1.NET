using BenchmarkDotNet.Running;
using fASN1.NET.Benchmark;

// Arguments select benchmarks and options, for example: --filter *Deserialize* --inProcess
BenchmarkSwitcher.FromAssembly(typeof(Bench).Assembly).Run(args);
