# Initial benchmark reference (baseline)

This file stores the short-run benchmark results captured locally for later comparison. The measurements were produced with the `IsVariantOfBenchmark` project (reduced warmup/iteration counts for quick runs).

Note: these are short-run numbers — run full BenchmarkDotNet runs for authoritative metrics.

| Framework | Scenario | Mean (ns) | Allocated (B) |
|---|---|---:|---:|
| .NET Framework 4.6.2 | Uncached IsVariantOf | 1,396.541 | 3,362 |
| .NET Framework 4.7   | Uncached IsVariantOf | 1,299.461 | 3,362 |
| .NET Framework 4.8   | Uncached IsVariantOf | 1,313.819 | 3,362 |
| .NET 8.0             | Uncached IsVariantOf | 865.385   | 784 |

| .NET Framework 4.6.2 | Cached IsVariantOf | (not separately reported) | (see uncached) |
| .NET Framework 4.7   | Cached IsVariantOf | 40.293    | 24 |
| .NET Framework 4.8   | Cached IsVariantOf | 40.438    | 24 |
| .NET 8.0             | Cached IsVariantOf | 12.268    | 24 |

| .NET Framework 4.7   | List instance IsInstanceOf | 46.795 | 24 |
| .NET Framework 4.8   | List instance IsInstanceOf | 45.035 | 24 |
| .NET 8.0             | List instance IsInstanceOf | 11.807 | 24 |

| .NET Framework 4.7   | Array instance IsInstanceOf | 61.906 | 24 |
| .NET Framework 4.8   | Array instance IsInstanceOf | 58.494 | 24 |
| .NET 8.0             | Array instance IsInstanceOf | 11.741 | 24 |

## How this file was produced
- Ran: `dotnet run -c Release --project TypeLogic.LiskovWingSubstitution.Benchmarks --framework net8.0 -- --filter *IsVariantOf*`
- Captured the BenchmarkDotNet output and stored representative mean and allocation values above.

## Usage
- Keep this file under version control to compare future benchmark runs after implementing optimizations.
- When adding new benchmark results, include a timestamp and the git commit SHA for traceability.
