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
        /// <summary>
        /// Cache of previously computed conversion results between a source and a target type.
        /// The key is a <see cref="VariantTypePair"/> and the value is a <see cref="ConversionInfo"/> describing
        /// whether conversion is possible and, when positive, the resolved runtime type to use for substitution.
        /// </summary>
        internal static readonly ConcurrentDictionary<VariantTypePair, ConversionInfo> _conversionCache = new ConcurrentDictionary<VariantTypePair, ConversionInfo>();

        // Reflection caches to avoid repeated reflection work
        private static readonly ConcurrentDictionary<Type, Type> _genericDefCache = new ConcurrentDictionary<Type, Type>();
        private static readonly ConcurrentDictionary<Type, Type[]> _genericArgsCache = new ConcurrentDictionary<Type, Type[]>();
        private static readonly ConcurrentDictionary<Type, Type[]> _interfacesCache = new ConcurrentDictionary<Type, Type[]>();

        // Cache to avoid scanning interfaces repeatedly: maps (type, genericDefinition) -> implemented interface type or negative sentinel
        private static readonly ConcurrentDictionary<VariantTypePair, Type> _implementedGenericInterfaceCache = new ConcurrentDictionary<VariantTypePair, Type>();
        // Negative sentinel to mark absence in cache (cannot store null in ConcurrentDictionary values)
        private static readonly Type _negativeSentinel = typeof(TypeExtensions);

        // Cache for results of SatisfiesTypeConstraints to avoid recomputation on recursive calls
        private static readonly ConcurrentDictionary<VariantTypePair, Type> _satisfiesConstraintsCache = new ConcurrentDictionary<VariantTypePair, Type>();
        // Cache for generic parameter constraints arrays
        private static readonly ConcurrentDictionary<Type, Type[]> _genericParamConstraintsCache = new ConcurrentDictionary<Type, Type[]>();

        /// <summary>
        /// Clears the internal conversion and reflection caches used by <see cref="IsVariantOf(Type, Type, out Type)"/>.
        /// </summary>
        /// <remarks>
        /// Intended for testing and benchmarking to force uncached execution paths. Clearing the cache is thread-safe
        /// but will cause subsequent calls to re-evaluate type relationships.
        /// </remarks>
        public static void ClearCache()
        {
            _conversionCache.Clear();
            _genericDefCache.Clear();
            _genericArgsCache.Clear();
            _interfacesCache.Clear();
            _implementedGenericInterfaceCache.Clear();
            _satisfiesConstraintsCache.Clear();
            _genericParamConstraintsCache.Clear();
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
            var expectedTypeGenericDefinition = GetGenericDefinitionCached(expectedType);
            var expectedTypeArguments = GetGenericArgumentsCached(expectedType);
            int expectedArgCount = expectedTypeArguments.Length;

            if (!expectedType.IsInterface)
            {
                // walk base types without LINQ, cache generic definition checks
                var current = type;
                while (current != null)
                {
                    if (current.IsGenericType)
                    {
                        var genericTypeDefinition = GetGenericDefinitionCached(current);
                        if (genericTypeDefinition == expectedTypeGenericDefinition)
                        {
                            var info = _conversionCache.GetOrAdd(cacheKey, k => ConversionInfo.Register(k, current));
                            runtimeType = info.RuntimeType;
                            return true;
                        }
                    }
                    current = current.BaseType;
                }
            }
            else
            {
                // If the type itself is a generic implementation of the expected generic interface
                if (type.IsGenericType)
                {
                    var typeGenDef = GetGenericDefinitionCached(type);
                    if (typeGenDef == expectedTypeGenericDefinition && SatisfiesTypeConstraints(type, expectedType, out runtimeType))
                    {
                        _conversionCache.TryAdd(cacheKey, ConversionInfo.Register(cacheKey, runtimeType));
                        return true;
                    }
                }

                // Use cache-backed lookup to avoid scanning all interfaces repeatedly
                Type implemented = GetImplementedGenericInterface(type, expectedTypeGenericDefinition, expectedArgCount);
                if (implemented != null)
                {
                    if (SatisfiesTypeConstraints(implemented, expectedType, out runtimeType))
                    {
                        _conversionCache.TryAdd(cacheKey, ConversionInfo.Register(cacheKey, runtimeType));
                        return true;
                    }
                }
            }

            _conversionCache.TryAdd(cacheKey, ConversionInfo.Negative);
            return false;
        }

        private static Type GetGenericDefinitionCached(Type t)
        {
            return _genericDefCache.GetOrAdd(t, key => key.IsGenericType ? key.GetGenericTypeDefinition() : null);
        }

        private static Type[] GetGenericArgumentsCached(Type t)
        {
            return _genericArgsCache.GetOrAdd(t, key => key.GetGenericArguments());
        }

        private static Type[] GetInterfacesCached(Type t)
        {
            return _interfacesCache.GetOrAdd(t, key => key.GetInterfaces());
        }

        /// <summary>
        /// Looks up (and caches) the interface implemented by <paramref name="type"/> that has the generic definition <paramref name="expectedGenericDefinition"/>.
        /// Returns null if no such interface is implemented by <paramref name="type"/>.
        /// </summary>
        private static Type GetImplementedGenericInterface(Type type, Type expectedGenericDefinition, int expectedArgCount)
        {
            if (expectedGenericDefinition == null) return null;
            var key = new VariantTypePair(type, expectedGenericDefinition);
            if (_implementedGenericInterfaceCache.TryGetValue(key, out var cached))
            {
                return cached == _negativeSentinel ? null : cached;
            }

            // Check if the type itself matches
            if (type.IsGenericType)
            {
                var def = GetGenericDefinitionCached(type);
                if (def == expectedGenericDefinition && GetGenericArgumentsCached(type).Length == expectedArgCount)
                {
                    _implementedGenericInterfaceCache.TryAdd(key, type);
                    return type;
                }
            }

            var interfaces = GetInterfacesCached(type);
            for (int i = 0; i < interfaces.Length; i++)
            {
                var iface = interfaces[i];
                if (!iface.IsGenericType) continue;
                var ifaceGenDef = GetGenericDefinitionCached(iface);
                if (ifaceGenDef != expectedGenericDefinition) continue;
                if (GetGenericArgumentsCached(iface).Length != expectedArgCount) continue;

                _implementedGenericInterfaceCache.TryAdd(key, iface);
                return iface;
            }

            _implementedGenericInterfaceCache.TryAdd(key, _negativeSentinel);
            return null;
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

            var cacheKey = new VariantTypePair(type, expectedType);
            if (_satisfiesConstraintsCache.TryGetValue(cacheKey, out var cachedResult))
            {
                if (cachedResult == _negativeSentinel)
                {
                    constrainedType = null;
                    return false;
                }
                constrainedType = cachedResult;
                return true;
            }

            Type[] genericTypeArguments = GetGenericArgumentsCached(type);
            Type[] expectedTypeArguments = GetGenericArgumentsCached(expectedType);

            //obvious cases
            if (genericTypeArguments.Length != expectedTypeArguments.Length)
            {
                _satisfiesConstraintsCache.TryAdd(cacheKey, _negativeSentinel);
                return false;
            }

            int l = genericTypeArguments.Length;
            var substitutedArgs = new Type[l];
            for (int i = 0; i < l; i++)
            {
                var typeArg = genericTypeArguments[i];
                var expectedTypeArg = expectedTypeArguments[i];

                if (!expectedTypeArg.IsGenericParameter)
                {
                    if (!typeArg.IsVariantOf(expectedTypeArg, out var substitutedArg))
                    {
                        _satisfiesConstraintsCache.TryAdd(cacheKey, _negativeSentinel);
                        return false;
                    }
                    substitutedArgs[i] = substitutedArg;
                }
                else
                {
                    var constraints = _genericParamConstraintsCache.GetOrAdd(expectedTypeArg, key => key.GetGenericParameterConstraints());
                    for (int c = 0; c < constraints.Length; c++)
                    {
                        if (!typeArg.IsVariantOf(constraints[c], out var _))
                        {
                            _satisfiesConstraintsCache.TryAdd(cacheKey, _negativeSentinel);
                            return false;
                        }
                    }
                    substitutedArgs[i] = typeArg;
                }
            }

            constrainedType = GetGenericDefinitionCached(expectedType).MakeGenericType(substitutedArgs);
            _satisfiesConstraintsCache.TryAdd(cacheKey, constrainedType);
            return true;
        }
    }
}