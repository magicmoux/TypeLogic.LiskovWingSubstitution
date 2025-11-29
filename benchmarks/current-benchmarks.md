# Current benchmark results

Command used:

```
dotnet run -c Release --project TypeLogic.LiskovWingSubstitution.Benchmarks/TypeLogic.LiskovWingSubstitution.Benchmarks.csproj -f net8.0 -- --filter *
```

Summary extracted from the most recent run (short local configuration):

| Scenario / Framework | Mean (ns) | Gen0 | Allocated (B) |
|---|---:|---:|---:|
| Uncached - .NET Framework 4.6.2 | 3,202.410 | 0.4120 | 5,416 B |
| Uncached - .NET Framework 4.7   | 3,183.205 | 0.4120 | 5,416 B |
| Uncached - .NET Framework 4.8   | 3,127.819 | 0.4120 | 5,416 B |
| Uncached - .NET 8.0             | 2,380.742 | 0.0954 | 1,280 B |
| Cached - .NET Framework 4.7     | 29.453    | 0.00   | ~56 B |
| List Instance - .NET Framework 4.7 | 32.643 | 0.00 | ~56 B |
| Array Instance - .NET Framework 4.7 | 44.004 | 0.00 | ~56 B |
| Cached - .NET Framework 4.8     | 29.535    | 0.00   | ~56 B |
| List Instance - .NET Framework 4.8 | 32.277 | 0.00 | ~56 B |
| Array Instance - .NET Framework 4.8 | 43.742 | 0.00 | ~56 B |
| Cached - .NET 8.0               | 5.107     | 0.00   | ~56 B |
| List Instance - .NET 8.0       | 5.873     | 0.00   | ~56 B |
| Array Instance - .NET 8.0      | 5.832     | 0.00   | ~56 B |

Notes
- These numbers come from a short local run (reduced warmup/iteration counts). Use full BenchmarkDotNet runs for reproducible measurements.
- The `Uncached` benchmarks were executed with internal caches cleared before each invocation to exercise the uncached path.
- Full BenchmarkDotNet output and artifacts are available under `BenchmarkDotNet.Artifacts/results/` in the repository.
