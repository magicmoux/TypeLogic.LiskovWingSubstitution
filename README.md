# TypeLogic.LiskovWingSubstitution

[![NuGet](https://img.shields.io/nuget/v/TypeLogic.LiskovWingSubstitution.svg)](https://www.nuget.org/packages/TypeLogic.LiskovWingSubstitution)
[![Downloads](https://img.shields.io/nuget/dt/TypeLogic.LiskovWingSubstitution.svg)](https://www.nuget.org/packages/TypeLogic.LiskovWingSubstitution)
[![.NET Standard](https://img.shields.io/badge/.NET%20Standard-2.0-blue.svg)](https://docs.microsoft.com/en-us/dotnet/standard/net-standard)
[![Tests](https://img.shields.io/badge/Tests-.NET%204.6.2--8.0-blue.svg)](https://dotnet.microsoft.com/)

A .NET library implementing the Liskov/Wing Substitution Principle for type variance checking (including generic typeParameters constraints validation). 

This project emerged from previous R&D tests dealing with .NET generics: I needed an additional simple way to verify type variance against GenericTypeDefinition, complementing .NET's built-in type system capabilities.

What started as a proof-of-concept to simplify variance checking turned into a full-fledged library that extends .NET's type system that you may find educational or even practicalin some cases. The implementation is based from Barbara Liskov's and Jeannette Wing's work on behavioral subtyping, providing additional variance checking capabilities when working with GenericTypeDefinitions (especially in cases where non-generic Type inheritance is absent, as illustrated in the examples).

## Features

- Type variance checking according to Liskov/Wing Substitution Principle
- Support for generic type parameter constraints
- Caching of type variance relationships for performance
- Instance-based variance checking
- Support for generic type definitions
- Full support for .NET Standard 2.0

## Installation

The package can be installed via NuGet:

```shell
dotnet add package TypeLogic.LiskovWingSubstitution
```

## Usage

### Basic Type Variance Checking

```csharp
using TypeLogic.LiskovWingSubstitution;

// Check if List<string> is variant of IEnumerable<object>
bool isVariant = typeof(List<string>).IsSubtypeOf(typeof(IEnumerable<object>));

// Check with runtime type information
bool isVariantWithType = typeof(List<string>).IsSubtypeOf(typeof(IEnumerable<object>), out Type runtimeType);
```

### Instance-Based Variance Checking

```csharp
using TypeLogic.LiskovWingSubstitution;

List<string> instance = new List<string>();

// Check with type parameter
bool isInstanceOfType = instance.IsInstanceOf(typeof(IEnumerable<object>), out var runtimeType);
// runtimeType should be IEnumerable<string>
```

```csharp
using TypeLogic.LiskovWingSubstitution;

string instance = "this is a string";

// Check with against generic type with the corresponding runtimeType
bool isInstanceOfType = instance.IsInstanceOf(typeof(IEquatable<>), out var runtimeType);
// runtimeType should be IEquatable<string>
```

### Generic Type Definition Support

```csharp
using TypeLogic.LiskovWingSubstitution;

// Check if List<T> is variant of IEnumerable<>
bool isGenericVariant = typeof(List<int>).IsSubtypeOf(typeof(IEnumerable<>));

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

## Contributing

Contributions are welcome! 
Please feel free to submit a Pull Request or open an issue in the repository with detailed steps to reproduce any bug or performance regression.

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## References

- Barbara Liskov — Liskov Substitution Principle (overview): https://en.wikipedia.org/wiki/Liskov_substitution_principle
- Barbara Liskov and Jeannette M. Wing — "A Behavioral Notion of Subtyping" (1994) PDF: https://www.cs.cmu.edu/~wing/publications/LiskovWing94.pdf

