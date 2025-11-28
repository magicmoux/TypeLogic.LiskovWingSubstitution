# TypeLogic.LiskovWingSubstitution

[![NuGet](https://img.shields.io/nuget/v/TypeLogic.LiskovWingSubstitution.svg)](https://www.nuget.org/packages/TypeLogic.LiskovWingSubstitution)
[![Downloads](https://img.shields.io/nuget/dt/TypeLogic.LiskovWingSubstitution.svg)](https://www.nuget.org/packages/TypeLogic.LiskovWingSubstitution)
[![.NET Standard](https://img.shields.io/badge/.NET%20Standard-2.0-blue.svg)](https://docs.microsoft.com/en-us/dotnet/standard/net-standard)
[![.NET Framework](https://img.shields.io/badge/.NET%20Framework-4.0%2B-blue.svg)](https://dotnet.microsoft.com/)
[![Tests](https://img.shields.io/badge/Tests-.NET%204.6.2--8.0-blue.svg)](https://dotnet.microsoft.com/)

A .NET library implementing the Liskov/Wing Substitution Principle for type variance checking (including generic typeParameters constraints validation). 

This project emerged from previous R&D tests dealing with .NET generics: I needed an additional simple way to verify type variance against GenericTypeDefinition, complementing .NET's built-in type system capabilities.

What started as an experiment  to simplify variance checking turned into a full-fledged library that extends .NET's type system that you may find educational or even practical in some cases. The implementation is based from Barbara Liskov's and Jeannette Wing's work on behavioral subtyping, providing additional variance checking capabilities when working with GenericTypeDefinitions (especially in cases where non-generic Type inheritance is absent, as illustrated in the examples).

## Features

- Type variance checking according to Liskov/Wing Substitution Principle
- Support for generic type parameter constraints
- Caching of type variance relationships for performance
- Instance-based variance checking
- Support for generic type definitions
- Full support for .NET Standard 2.0 and .NET Framework 4.0+

## Installation

The package can be installed via NuGet:

```shell
dotnet add package TypeLogic.LiskovWingSubstitution
```

## Usage

### Basic Type Variance Checking

```csharp
using TypeLogic.LiskovWingSubstitutions;

// Check if List<string> is variant of IEnumerable<object>
bool isVariant = typeof(List<string>).IsVariantOf(typeof(IEnumerable<object>));

// Check with runtime type information
bool isVariantWithType = typeof(List<string>).IsVariantOf(typeof(IEnumerable<object>), out Type runtimeType);
```

### Instance-Based Variance Checking

```csharp
using TypeLogic.LiskovWingSubstitutions;

List<string> instance = new List<string>();

// Check with type parameter
bool isInstanceOfType = instance.IsInstanceOf(typeof(IEnumerable<object>), out var runtimeType);
// runtimeType should be IEnumerable<string>
```

```csharp
using TypeLogic.LiskovWingSubstitutions;

string instance = "this is a string";

// Check with against generic type with the corresponding runtimeType
bool isInstanceOfType = instance.IsInstanceOf(typeof(IEquatable<>), out var runtimeType);
// runtimeType should be IEquatable<string>
```

### Generic Type Definition Support

```csharp
using TypeLogic.LiskovWingSubstitutions;

// Check if List<T> is variant of IEnumerable<>
bool isGenericVariant = typeof(List<int>).IsVariantOf(typeof(IEnumerable<>));

// Convert instance based on generic definition
string instance = "test";
var converted = instance.ConvertAs(typeof(IEnumerable<>)); // Converts to IEnumerable<char>
```

## Features in Detail

### Type Variance Checking

The library implements comprehensive type variance checking:

- Direct type equality
- Interface and inheritance hierarchy traversal
- Generic type parameter constraints validation
- Support for generic type definitions
- Cached type relationship checks

### Instance Conversion

_still in progress_

Provides safe instance conversion with variance checking:

- Runtime type checking
- Generic type parameter validation
- Type constraint satisfaction verification
- Conversion caching for performance

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

## Performance

The library includes comprehensive benchmarks to measure performance across different scenarios. Below are short-run results produced locally (reduced iterations for quicker runs). These numbers are illustrative and will vary by machine, runtime and configuration; run the benchmarks locally with the provided Benchmark project for full, reproducible results.

Benchmark configuration used for this run:
- Release configuration
- Reduced warmup/iteration counts for quick local runs
- Frameworks: .NET Framework 4.6.2, 4.7, 4.8 and .NET 8.0

Summary (mean times)

| Scenario / Framework | .NET 4.6.2 | .NET 4.7 | .NET 4.8 | .NET 8.0 |
|---|---:|---:|---:|---:|
| Uncached IsVariantOf | ~1,298 ns | ~1,240 ns | ~1,268 ns | ~781 ns |
| Cached IsVariantOf | ~42 ns | ~42 ns | ~44 ns | ~13 ns |
| List instance IsInstanceOf | ~48 ns | ~48 ns | ~48 ns | ~12 ns |
| Array instance IsInstanceOf | ~62 ns | ~62 ns | ~61 ns | ~12 ns |

Allocations per operation (approx)
- Uncached IsVariantOf: ~784–2985 B (depends on scenario/framework)
- Cached / Instance checks: ~24 B (mostly negligible)

Notes
- The uncached code path involves reflective scanning and generic-constraint checking; caching dramatically reduces both latency and allocations.
- These short-run numbers were produced with the benchmarks configured for fast execution; for reliable/production-grade metrics run the Benchmark project with the default BenchmarkDotNet configuration (more iterations and longer warmup).

To reproduce the full benchmarks:

```bash
dotnet run -c Release --project TypeLogic.LiskovWingSubstitution.Benchmarks
```

## References

This project is based on the work of Barbara Liskov and Jeannette Wing on the Substitution Principle and behavioral subtyping.

- [Barbara Liskov's Substitution Principle](https://en.wikipedia.org/wiki/Liskov_substitution_principle)
- [Jeannette Wing's work on behavioral subtyping](https://www.cs.cmu.edu/~wing/publications/LiskovWing94.pdf)
