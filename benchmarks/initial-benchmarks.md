Initial benchmark results for TypeLogic.LiskovWingSubstitution

Command: dotnet run -c Release --project TypeLogic.LiskovWingSubstitution.Benchmarks/TypeLogic.LiskovWingSubstitution.Benchmarks.csproj -f net8.0 -- --filter *

Summary of key results (extracted):

| Benchmark         | Target Runtime | Mean (ns) | Error (ns) | StdDev (ns) | Gen0 | Allocated (B) |
|------------------:|----------------:|----------:|-----------:|------------:|----:|--------------:|
| IsSubtypeOf Uncached (baseline) - .NET 4.6.2 | .NET Framework 4.6.2 | 451,422 ns | 30,776 ns | 1,686.9 ns | 0.0610 | 802 B |
| IsSubtypeOf Cached - .NET 8.0 | .NET 8.0 | 9.187 ns | 1.1506 ns | 0.0631 ns | 0.0043 | 56 B |
| Uncached - .NET 8.0 | .NET 8.0 | 297.112 ns | 43.6995 ns | 2.3953 ns | 0.0658 | 864 B |

Full output logged in BenchmarkDotNet artifacts directory produced by the run.
