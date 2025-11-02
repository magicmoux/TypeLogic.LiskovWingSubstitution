# TypeLogic.LiskovWingSubstitution

[![NuGet](https://img.shields.io/badge/nuget-not%20yet%20available-lightgrey.svg)](https://www.nuget.org/packages/TypeLogic.LiskovWingSubstitution)
[![.NET Standard](https://img.shields.io/badge/.NET%20Standard-2.0-blue.svg)](https://docs.microsoft.com/en-us/dotnet/standard/net-standard)
[![.NET Framework](https://img.shields.io/badge/.NET%20Framework-4.0%2B-blue.svg)](https://dotnet.microsoft.com/)
[![Tests](https://img.shields.io/badge/Tests-.NET%204.6.2--8.0-blue.svg)](https://dotnet.microsoft.com/)

A .NET library implementing the Liskov/Wing Substitution Principle for type variance checking (including generic typeParameters constraints validation). This project emerged from previous R&D tests dealing with .NET generics: I needed an additional simple way to verify type variance against GenericTypeDefinition, complementing .NET's built-in type system capabilities.

What started as a proof-of-concept to simplify variance checking turned into a full-fledged library that extends .NET's type system. The implementation draws inspiration from Barbara Liskov's and Jeannette Wing's work on behavioral subtyping, providing additional variance checking capabilities when working with GenericTypeDefinitions (especially in cases where non-generic Type inheritance is absent, as illustrated in the examples).

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

// Check if instance is variant of IEnumerable<object>
bool isInstanceOf = instance.IsInstanceOf<List<string>, IEnumerable<object>>();

// Check with type parameter
bool isInstanceOfType = instance.IsInstanceOf(typeof(IEnumerable<object>));
```

### Generic Type Definition Support

```csharp
using TypeLogic.LiskovWingSubstitutions;

// Check if List<> is variant of IEnumerable<>
bool isGenericVariant = typeof(List<>).IsVariantOf(typeof(IEnumerable<>));

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

The library includes comprehensive benchmarks to measure performance across different scenarios:

- Type variance checking with and without caching
- Instance-based variance checking
- Generic type definition handling
- Type constraint validation

To run the benchmarks:

```shell
dotnet run -c Release --project TypeLogic.LiskovWingSubstitution.Benchmarks
```

## Related Projects

- [Barbara Liskov's Substitution Principle](https://en.wikipedia.org/wiki/Liskov_substitution_principle)
- [Jeannette Wing's work on behavioral subtyping](https://www.cs.cmu.edu/~wing/publications/LiskovWing94.pdf)

## Acknowledgments

This project is inspired by the work of Barbara Liskov and Jeannette Wing on the Substitution Principle and behavioral subtyping.
