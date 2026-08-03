using BenchmarkDotNet.Running;
using SimpleTypeScript.Benchmarks;

// The figures behind the ceilings in `tests/SimpleTypeScript.Tests/Performance`. Those hold the shape of the
// cost on every build; these say what it actually is, which is the question worth asking before changing how
// something is written.
BenchmarkSwitcher.FromAssembly(typeof(EmitterBenchmarks).Assembly).Run(args);
