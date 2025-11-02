# TypeLogic.LiskovWingSubstitution

A .NET library implementing the Liskov/Wing Substitution Principle for type variance checking (including type parameter constraint validation). This project emerged from previous R&D tests dealing with .NET generics complexities : I needed a reliable library to simply check that type Variance is applicable against a GenericTypeDefinition, which led to this implementation of the Liskov/Wing Substitution Principle.

What started as a proof-of-concept to simplify variance checking turned into a full-fledged library that I hope others will find both educational and practical. The implementation draws inspiration from Barbara Liskov's and Jeannette Wing's work on behavioral subtyping, translating their theoretical foundations into practical .NET code that is not handled by the .NET TypeIsAssignableFrom when dealing with GenericTypeDefinitions (especially when there is no non-generic Type).

## Overview

The main goal of this library is to extend .NET's type system capabilities by providing advanced type variance checking that goes beyond what's possible with the native `Type.IsAssignableFrom` method. Specifically, it determines whether a runtime type is variant of another type while properly handling:

- Complex generic type constraints that the standard .NET type system doesn't fully validate
- Variance relationships between generic type definitions
- Deep type parameter constraint satisfaction verification
- Cases where standard assignability checks would fail but valid variance relationships exist

This comprehensive approach ensures proper type substitutability according to the Liskov/Wing Substitution Principle, especially in scenarios involving generic type parameters and their constraints.

For example, while `Type.IsAssignableFrom` might fail to recognize certain valid variance relationships between complex generic types, this library will correctly identify cases where safe type substitution is possible while still respecting all type constraints.

The library provides a set of extension methods to check these variance relationships and handles both simple inheritance and complex generic type constraints through a carefully optimized implementation.

## Features

- Type variance checking according to Liskov/Wing Substitution Principle
- Support for generic type parameter constraints
- Caching of type variance relationships for performance
- Instance-based variance checking
- Support for generic type definitions
- Full support for .NET Standard 2.0 and .NET Framework 4.0+

## Installation

The package is not yet available

## Usage

### Basic Type Variance Checking

```csharp
using TypeLogic.LiskovWingSubstitutions;

// Check if List<string> is variant of IEnumerable<object>
bool isVariant = typeof(List<string>).IsVariantOf(typeof(IEnumerable<object>));

// Check with runtime type information
bool isVariantWithType = typeof(List<string>).IsVariantOf(typeof(IEnumerable<object>), out Type runtimeType);
// runtimeType should be IEnumerable<string>
```

### Instance-Based Variance Checking

```csharp
using TypeLogic.LiskovWingSubstitutions;

string instance = "This is a string";

// Check against a generic Interface that has no non-generic definition
bool isInstanceOfType = instance.IsInstanceOf(typeof(IEnumerable<>), out var runtimeType);
// runtimeType should be IEnumerable<char>

// Check against a generic Interface that has no non-generic definition
bool isInstanceOfType = instance.IsInstanceOf(typeof(IEquatable<>));
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

Still in work

### Performance Optimization

The library includes several performance optimizations:

- Type relationship caching
- Lazy delegate compilation
- Efficient type constraint validation
- Optimized generic type handling

## Target Frameworks

- .NET Standard 2.0
- .NET Framework 4.0
- .NET Framework 4.6.2
- .NET Framework 4.7
- .NET Framework 4.8
- .NET 8.0 (for tests and benchmarks)

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

## References

- [Barbara Liskov's Substitution Principle](https://en.wikipedia.org/wiki/Liskov_substitution_principle)
- [Jeannette Wing's work on behavioral subtyping](https://www.cs.cmu.edu/~wing/publications/LiskovWing94.pdf)

## Acknowledgments

This project is inspired by the work of Barbara Liskov and Jeannette Wing on the Substitution Principle and behavioral subtyping.
