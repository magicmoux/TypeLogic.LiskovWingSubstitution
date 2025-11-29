Initial benchmark results for TypeLogic.LiskovWingSubstitution

Command: dotnet run -c Release --project TypeLogic.LiskovWingSubstitution.Benchmarks/TypeLogic.LiskovWingSubstitution.Benchmarks.csproj -f net8.0 -- --filter *

Summary of key results (extracted from the latest run):

| Benchmark / Scenario | Target Runtime | Mean (ns) | Gen0 | Allocated (B) |
|---------------------:|---------------:|----------:|----:|--------------:|
| Cached (fast path)  | .NET Framework 4.6.2 | 31.556 ns  | 0.00 | 56 B |
| List Instance       | .NET Framework 4.6.2 | 35.917 ns  | 0.00 | 56 B |
| Array Instance      | .NET Framework 4.6.2 | 46.109 ns  | 0.00 | 56 B |
| Uncached            | .NET Framework 4.6.2 | 422.691 ns | 0.0610 | 802 B |
| Cached (fast path)  | .NET Framework 4.7   | 31.527 ns  | 0.00 | 56 B |
| List Instance       | .NET Framework 4.7   | 35.297 ns  | 0.00 | 56 B |
| Array Instance      | .NET Framework 4.7   | 46.760 ns  | 0.00 | 56 B |
| Uncached            | .NET Framework 4.7   | 432.038 ns | 0.0610 | 802 B |
| Cached (fast path)  | .NET Framework 4.8   | 31.483 ns  | 0.00 | 56 B |
| List Instance       | .NET Framework 4.8   | 35.077 ns  | 0.00 | 56 B |
| Array Instance      | .NET Framework 4.8   | 46.058 ns  | 0.00 | 56 B |
| Uncached            | .NET Framework 4.8   | 439.167 ns | 0.0610 | 802 B |
| Cached (fast path)  | .NET 8.0             | 8.287 ns   | 0.03 | 56 B |
| List Instance       | .NET 8.0             | 8.461 ns   | 0.03 | 56 B |
| Array Instance      | .NET 8.0             | 8.345 ns   | 0.03 | 56 B |
| Uncached            | .NET 8.0             | 275.596 ns | 0.0658 | 864 B |

Full BenchmarkDotNet output and artifacts are available in the generated `BenchmarkDotNet.Artifacts/results/` directory produced by the run.

Notes
- These numbers are from a short, local run (reduced warmup/iteration counts configured for quick feedback). For reproducible comparisons use a full BenchmarkDotNet run with the default/recommended configuration.
- The `Uncached` rows correspond to the baseline (Benchmark attribute Baseline = true) runs that force the uncached path by clearing internal caches before each invocation.
- `Cached` rows show the cached-path performance when conversion/cache delegates are available.
