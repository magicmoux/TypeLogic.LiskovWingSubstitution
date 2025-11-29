# TypeLogic.LiskovWingSubstitution

[![NuGet](https://img.shields.io/nuget/v/TypeLogic.LiskovWingSubstitution.svg)](https://www.nuget.org/packages/TypeLogic.LiskovWingSubstitution)
[![Downloads](https://img.shields.io/nuget/dt/TypeLogic.LiskovWingSubstitution.svg)](https://www.nuget.org/packages/TypeLogic.LiskovWingSubstitution)
[![.NET Standard](https://img.shields.io/badge/.NET%20Standard-2.0-blue.svg)](https://docs.microsoft.com/en-us/dotnet/standard/net-standard)
[![.NET Framework](https://img.shields.io/badge/.NET%20Framework-4.0%2B-blue.svg)](https://dotnet.microsoft.com/)
[![Tests](https://img.shields.io/badge/Tests-.NET%204.6.2--8.0-blue.svg)](https://dotnet.microsoft.com/)

A .NET library to check behavioral subtyping following the Liskov/Wing definition of substitutability (including generic type parameter constraints validation). 

This project emerged from previous R&D tests dealing with .NET generics: I needed a minimal api to check substitutability against GenericTypes and GenericTypeDefinitions, complementing .NET's built-in type system capabilities.

What started as an experiment to simplify checking subtyping validity turned into a full-fledged library that extends .NET's type system. The implementation is based on Barbara Liskov's and Jeannette Wing's work on behavioral subtyping and provides additional checking capabilities when working with GenericTypeDefinitions (especially in cases where non-generic Type inheritance is absent, as exposed by some examples).

## Features

- Behavioral subtype checking following the Liskov/Wing definition of substitutability
- Support for generic type parameter constraints
- Caching of subtype relationships for performance
- Instance-based subtype checking
- Support for generic type definitions
- Full support for .NET Standard 2.0 and .NET Framework 4.0+

The library supports comprehensive behavioral subtype checking:

- Direct type equality
- Interface and inheritance hierarchy traversal
- Generic type parameter constraints validation
- Support for generic type definitions
- Cached subtype relationship checks

## Usage

### Installation

The package can be installed via NuGet:

```shell
dotnet add package TypeLogic.LiskovWingSubstitution
```

### Basic Behavioral Subtype Checking

```csharp
using TypeLogic.LiskovWingSubstitutions;

// Check if List<string> can be used in place of IEnumerable<object>
bool isSubtype = typeof(List<string>).IsSubtypeOf(typeof(IEnumerable<object>));

// Check subtyping and provides the runtimeType substitute
bool isSubtypeWithRuntimeType = typeof(List<string>).IsSubtypeOf(typeof(IEnumerable<object>), out Type runtimeType);
```

### Instance-Based Subtype Checking

```csharp
using TypeLogic.LiskovWingSubstitutions;

List<string> instance = new List<string>();

// Check subtyping and provides the runtimeType substitute
bool isInstanceOfIEnumerable = instance.IsInstanceOf(typeof(IEnumerable<object>), out var runtimeType);
// runtimeType should be IEnumerable<string>
```

```csharp
using TypeLogic.LiskovWingSubstitutions;

string instance = "this is a string";

// Check subtyping against a generic interface definition
bool isInstanceOfIEquatable = instance.IsInstanceOf(typeof(IEquatable<>), out var runtimeType);
// runtimeType should be IEquatable<string>
```

### Generic Type Definition Support

```csharp
using TypeLogic.LiskovWingSubstitutions;

// Check if List<T> can be considered a subtype of IEnumerable<>
bool isSubtype = typeof(List<int>).IsSubtypeOf(typeof(IEnumerable<>));
```

### Instance conversion into closest runtime substitute type

_still in progress_

Provides safe instance conversion with behavioral checks:

- Runtime type checking
- Generic type parameter validation
- Type constraint satisfaction verification
- Conversion caching for performance

Example

```csharp
// Convert instance based on generic definition
string instance = "test";
var converted = instance.ConvertAs(typeof(IEnumerable<>)); 
// Should convert into an IEnumerable<char>
```

### Performance Optimization

The library includes several performance optimizations:

- Type relationship caching
- Lazy delegate compilation
- Efficient type constraint validation
- Optimized generic type handling

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Performance — comparison vs initial baseline

This section summarizes the short-run benchmark comparison between the latest run (`benchmarks/current-benchmarks.md`) and the reference initial run (`benchmarks/initial-benchmarks.md`). The initial run is used as the reference unit; deltas are shown as percent change and multiplicative factor (Current / Initial).

Summary (Initial ? Current):

| Scenario / Framework | Initial (ns) | Current (ns) | Change | Factor (Current / Initial) |
|---|---:|---:|---:|---:|
| Uncached (baseline) — .NET Framework 4.6.2 | 422.69 | 3,202.41 | +657.6% | 7.58× |
| Uncached (baseline) — .NET Framework 4.7   | 432.04 | 3,183.21 | +636.9% | 7.37× |
| Uncached (baseline) — .NET Framework 4.8   | 439.17 | 3,127.82 | +612.3% | 7.12× |
| Uncached (baseline) — .NET 8.0             | 275.60 | 2,380.74 | +763.7% | 8.64× |
| Cached (fast path) — .NET Framework 4.7    | 31.53  | 29.45   | ?6.6%  | 0.93× |
| Cached (fast path) — .NET Framework 4.8    | 31.48  | 29.54   | ?6.2%  | 0.94× |
| Cached (fast path) — .NET 8.0              | 8.29   | 5.11    | ?38.4% | 0.62× |
| List-instance — .NET Framework 4.7         | 35.30  | 32.64   | ?7.5%  | 0.93× |
| List-instance — .NET Framework 4.8         | 35.08  | 32.28   | ?8.0%  | 0.92× |
| List-instance — .NET 8.0                   | 8.46   | 5.87    | ?30.6% | 0.69× |
| Array-instance — .NET Framework 4.7        | 46.76  | 44.00   | ?5.9%  | 0.94× |
| Array-instance — .NET Framework 4.8        | 46.06  | 43.74   | ?5.0%  | 0.95× |
| Array-instance — .NET 8.0                  | 8.35   | 5.83    | ?30.1% | 0.70× |

Notes
- Initial / Current values are taken from `benchmarks/initial-benchmarks.md` and `benchmarks/current-benchmarks.md` respectively.
- Both runs were short, local runs (reduced warmup/iteration counts). Use a full BenchmarkDotNet configuration for stable, reproducible numbers.
- Large regressions on the uncached path indicate substantial runtime/alloc differences between runs; investigate benchmark configuration, GC, and environmental noise before assuming a code regression.
- Where a scenario is missing from the current run it is omitted from the table (marked N/A if present in only one file).
- Full artifacts for both runs are under `BenchmarkDotNet.Artifacts/results/` and the extracted summaries are in `benchmarks/`.
