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

- ## Features in Detail

### Behavioral Subtype Checking

The library implements comprehensive behavioral subtype checking:

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

## Maintenance

Recent cleanup: removed unused `using` directives in several source files and replaced a small LINQ usage with an equivalent loop to reduce unnecessary dependencies. The solution builds after these changes.

## Performance (updated)

Short-run benchmark results (current run) vs initial baseline in `benchmarks-ref/initial-benchmarks.md`.

| Scenario / Framework | Baseline (ns) | Current (ns) | Delta |
|---|---:|---:|---:|
| Uncached IsSubtypeOf - .NET 4.6.2 | 1396.541 | 1892.235 | -35.5% (regression) |
| Uncached IsSubtypeOf - .NET 4.7   | 1299.461 | 1909.931 | -47.0% (regression) |
| Uncached IsSubtypeOf - .NET 4.8   | 1313.819 | 1957.479 | -49.0% (regression) |
| Uncached IsSubtypeOf - .NET 8.0   | 865.385  | 1287.775 | -48.8% (regression) |
| Cached IsSubtypeOf - .NET 4.7     | 40.293   | 36.782  | +8.7% improvement |
| Cached IsSubtypeOf - .NET 4.8     | 40.438   | 35.957  | +11.2% improvement |
| Cached IsSubtypeOf - .NET 8.0     | 12.268   | 7.813   | +36.3% improvement |

Notes
- These are short-run, locally produced numbers. Use full BenchmarkDotNet runs for stable results.
- Baseline data is taken from `benchmarks-ref/initial-benchmarks.md`.

## References

This project is based on the work of Barbara Liskov and Jeannette Wing on the Substitution Principle and behavioral subtyping.

- [Barbara Liskov's Substitution Principle](https://en.wikipedia.org/wiki/Liskov_substitution_principle)
- [Jeannette Wing's work on behavioral subtyping](https://www.cs.cmu.edu/~wing/publications/LiskovWing94.pdf)
