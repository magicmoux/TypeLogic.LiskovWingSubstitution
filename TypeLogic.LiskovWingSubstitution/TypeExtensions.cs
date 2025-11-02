using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace TypeLogic.LiskovWingSubstitutions
{
    /// <summary>
    /// Provides extension methods to check type variance relationships according to the Liskov/Wing Substitution Principle.
    /// </summary>
    public static class TypeExtensions
    {
        internal static readonly ConcurrentDictionary<VariantTypePair, ConversionInfo> _conversionCache = new ConcurrentDictionary<VariantTypePair, ConversionInfo>();

        // Added for benchmarking purposes
        public static void ClearCache()
        {
            _conversionCache.Clear();
        }

#if NET45_OR_GREATER || NETSTANDARD2_0
        /// <summary>
        /// Determines whether a runtime type is a variant type (according to the Liskov/Wing Substitution Principle) of the expected type.
        /// </summary>
        /// <param name="type">The Type to check for variance relationship.</param>
        /// <param name="expectedType">The target type to check variance against.</param>
        /// <returns>True if the type is variant of the expected type, false otherwise.</returns>
        public static bool IsVariantOf(this TypeInfo type, Type expectedType)
        {
            return IsVariantOf(type.AsType(), expectedType, out var substitutionType);
        }

        /// <summary>
        /// Determines whether a runtime type is a variant type (according to the Liskov Substitution Principle) of the expected type
        /// and returns the first valid substitution type if possible.
        /// </summary>
        /// <param name="type">The Type to check for variance relationship.</param>
        /// <param name="expectedType">The target type to check variance against.</param>
        /// <param name="substitutionType">When this method returns, contains the type that can be substituted for the expected type, if found; otherwise, null.</param>
        /// <returns>True if a valid substitution type is found, false otherwise.</returns>
        public static bool IsVariantOf(this TypeInfo type, Type expectedType, out Type substitutionType)
        {
            return IsVariantOf(type.AsType(), expectedType, out substitutionType);
        }
#endif

        /// <summary>
        /// Determines whether a runtime type is a variant type (according to the Liskov Substitution Principle) of the expected type.
        /// </summary>
        /// <param name="type">The Type to check for variance relationship.</param>
        /// <param name="expectedType">The target type to check variance against.</param>
        /// <returns>True if the type is variant of the expected type, false otherwise.</returns>
        public static bool IsVariantOf(this Type type, Type expectedType)
        {
            return IsVariantOf(type, expectedType, out var substitutionType);
        }

        /// <summary>
        /// Determines whether a runtime type is a variant type (according to the Liskov Substitution Principle) of the expected type
        /// and returns the first valid substitution type if possible.
        /// </summary>
        /// <param name="type">The Type to check for variance relationship.</param>
        /// <param name="expectedType">The target type to check variance against.</param>
        /// <param name="runtimeType">When this method returns, contains the type that can be substituted for the expected type, if found; otherwise, null.</param>
        /// <returns>True if a valid substitution type is found, false otherwise.</returns>
        /// <remarks>
        /// This method implements the core type variance checking logic, including:
        /// - Direct type equality checking
        /// - Cache-based lookup for previously checked type pairs
        /// - Generic type parameter constraints validation
        /// - Interface and inheritance hierarchy traversal
        /// </remarks>
        public static bool IsVariantOf(this Type type, Type expectedType, out Type runtimeType)
        {
            runtimeType = null;

            // obvious cases
            if (type == null || expectedType == null) return false;
            if (type == expectedType)
            {
                runtimeType = type;
                return true;
            }

            var cacheKey = new VariantTypePair(type, expectedType);

            // fast path: cached result (positive or negative)
            if (_conversionCache.TryGetValue(cacheKey, out var cached))
            {
                if (!cached.IsConvertible) return false;
                runtimeType = cached.RuntimeType;
                return true;
            }

            // non-generic open types cannot be converted to a closed expected type
            if (type.ContainsGenericParameters && !expectedType.ContainsGenericParameters)
            {
                _conversionCache.TryAdd(cacheKey, ConversionInfo.Negative);
                return false;
            }

            // native .NET inheritance checking
            if (expectedType.IsAssignableFrom(type))
            {
                var info = _conversionCache.GetOrAdd(cacheKey, k => ConversionInfo.Register(k, expectedType));
                runtimeType = info.RuntimeType;
                return true;
            }

            // At this point we should safely assume we deal with generic types only
            var expectedTypeGenericDefinition = expectedType.IsGenericType ? expectedType.GetGenericTypeDefinition() : null;
            var expectedTypeArguments = expectedType.GetGenericArguments();
            if (!expectedType.IsInterface)
            {
                do
                {
                    if (type.IsGenericType)
                    {
                        var genericTypeDefinition = type.GetGenericTypeDefinition();
                        if (genericTypeDefinition == expectedTypeGenericDefinition)
                        {
                            var info = _conversionCache.GetOrAdd(cacheKey, k => ConversionInfo.Register(k, type));
                            runtimeType = info.RuntimeType;
                            return true;
                        }
                    }
                    type = type.BaseType;
                } while (type != null);
            }
            else
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == expectedTypeGenericDefinition && SatisfiesTypeConstraints(type, expectedType, out runtimeType))
                {
                    _conversionCache.TryAdd(cacheKey, ConversionInfo.Register(cacheKey, runtimeType));
                    return true;
                }
                var potentialMatches = type.GetInterfaces()
                    .Where(iface => iface.IsGenericType
                        && expectedTypeGenericDefinition == iface.GetGenericTypeDefinition()
                        && iface.GetGenericArguments().Count() == expectedTypeArguments.Count());
                foreach (var iface in potentialMatches)
                {
                    if (iface.GetGenericTypeDefinition().IsVariantOf(expectedTypeGenericDefinition) && SatisfiesTypeConstraints(iface, expectedType, out runtimeType))
                    {
                        _conversionCache.TryAdd(cacheKey, ConversionInfo.Register(cacheKey, runtimeType));
                        return true;
                    }
                }
            }
            _conversionCache.TryAdd(cacheKey, ConversionInfo.Negative);
            return false;
        }

        /// <summary>
        /// Checks whether a type satisfies the generic parameter constraints of the expected type.
        /// </summary>
        /// <param name="type">The type to check against the constraints.</param>
        /// <param name="expectedType">The type containing the generic parameter constraints.</param>
        /// <param name="constrainedType">When this method returns, contains the constructed generic type that satisfies all constraints, if successful; otherwise, null.</param>
        /// <returns>True if all generic parameter constraints are satisfied, false otherwise.</returns>
        /// <remarks>
        /// This method validates that:
        /// - The number of generic arguments matches between the types
        /// - Each generic argument satisfies its corresponding constraints
        /// - For non-generic parameters, proper variance relationships are maintained
        /// </remarks>
        private static bool SatisfiesTypeConstraints(Type type, Type expectedType, out Type constrainedType)
        {
            constrainedType = null;

            Type[] genericTypeArguments = type.GetGenericArguments();
            Type[] expectedTypeArguments = expectedType.GetGenericArguments();

            //obvious cases
            if (genericTypeArguments.Count() != expectedTypeArguments.Count()) return false;

            var substitutedArgs = new List<Type>();
            for (int i = 0, l = genericTypeArguments.Count(); i < l; i++)
            {
                var typeArg = genericTypeArguments.ElementAt(i);
                var expectedTypeArg = expectedTypeArguments.ElementAt(i);
                if (!expectedTypeArg.IsGenericParameter)
                {
                    if (!typeArg.IsVariantOf(expectedTypeArg, out var substitutedArg)) return false;
                    substitutedArgs.Add(substitutedArg);
                }
                else if (expectedTypeArg.IsGenericParameter)
                {
                    foreach (var typeContraint in expectedTypeArg.GetGenericParameterConstraints())
                    {
                        if (!typeArg.IsVariantOf(typeContraint, out var substitutedArg)) return false;
                    }
                    substitutedArgs.Add(typeArg);
                }
            }
            constrainedType = expectedType.GetGenericTypeDefinition().MakeGenericType(substitutedArgs.ToArray());
            return true;
        }
    }
}