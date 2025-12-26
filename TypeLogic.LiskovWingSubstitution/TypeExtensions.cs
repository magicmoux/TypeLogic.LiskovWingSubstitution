using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace TypeLogic.LiskovWingSubstitution
{
    /// <summary>
    /// Provides extension methods to check type variance relationships according to the Liskov/Wing Substitution Principle.
    /// </summary>
    public static class TypeExtensions
    {
        internal static readonly ConcurrentDictionary<TypePair, Type> _runtimeSubstituteTypesCache = new ConcurrentDictionary<TypePair, Type>();

        // Added for benchmarking purposes
        public static void ClearCache()
        {
            _runtimeSubstituteTypesCache.Clear();
        }

#if NET45_OR_GREATER || NETSTANDARD2_0

        /// <summary>
        /// Determines whether a runtime type is a variant of the expected type according to the library's variance rules.
        /// </summary>
        /// <param name="type">The runtime type represented as a <see cref="TypeInfo"/> to check.</param>
        /// <param name="expectedType">The target type to check variance against.</param>
        /// <returns>True if the runtime type can be used as a variant of the expected type; otherwise false.</returns>
        public static bool IsSubtypeOf(this TypeInfo type, Type expectedType)
        {
            return IsSubtypeOf(type.AsType(), expectedType, out _);
        }

        /// <summary>
        /// Determines whether a runtime type is a variant of the expected type and returns a concrete substitution type when found.
        /// </summary>
        /// <param name="type">The runtime type represented as a <see cref="TypeInfo"/> to check.</param>
        /// <param name="expectedType">The target type to check variance against.</param>
        /// <param name="substitutionType">When this method returns, contains the type that can substitute <paramref name="expectedType"/>, or null if none was found.</param>
        /// <returns>True if a valid substitution type is found; otherwise false.</returns>
        public static bool IsSubtypeOf(this TypeInfo type, Type expectedType, out Type substitutionType)
        {
            return IsSubtypeOf(type.AsType(), expectedType, out substitutionType);
        }

#endif

        /// <summary>
        /// Determines whether a runtime type is a variant of the expected type according to the library's variance rules.
        /// </summary>
        /// <param name="type">The runtime <see cref="Type"/> to check.</param>
        /// <param name="expectedType">The target type to check variance against.</param>
        /// <returns>True if the runtime type can be used as a variant of the expected type; otherwise false.</returns>
        public static bool IsSubtypeOf(this Type type, Type expectedType)
        {
            return IsSubtypeOf(type, expectedType, out _);
        }

        /// <summary>
        /// Determines whether a runtime type is a variant of the expected type and returns the concrete substitution type when available.
        /// </summary>
        /// <param name="type">The runtime <see cref="Type"/> to check.</param>
        /// <param name="expectedType">The target type to check variance against.</param>
        /// <param name="runtimeType">When this method returns, contains the concrete type that can substitute <paramref name="expectedType"/>, or null if no substitution exists.</param>
        /// <returns>True if a valid substitution type is found; otherwise false.</returns>
        /// <remarks>
        /// This method implements the core type variance checking logic, including:
        /// - Direct type equality checking
        /// - Cache-based lookup for previously checked type pairs
        /// - Generic type parameter constraints validation
        /// - Interface and inheritance hierarchy traversal
        /// </remarks>
        public static bool IsSubtypeOf(this Type type, Type expectedType, out Type runtimeType)
        {
            runtimeType = null;

            // obvious cases
            if (type == null || expectedType == null) return false;
            if (type == expectedType)
            {
                runtimeType = type;
                return true;
            }

            var cacheKey = new TypePair(type, expectedType);

            // fast path: cached result (positive or negative)
            if (_runtimeSubstituteTypesCache.TryGetValue(cacheKey, out var cached))
            {
                runtimeType = cached;
                return cached != null;
            }

            // expected type is not an interface or a generic type
            if (!expectedType.IsGenericType)
            {
                if (!type.ContainsGenericParameters)
                    if (expectedType.IsAssignableFrom(type))
                    {
                        runtimeType = _runtimeSubstituteTypesCache.GetOrAdd(cacheKey, k => type);
                        return true;
                    }
                    else
                    {
                        _runtimeSubstituteTypesCache.TryAdd(cacheKey, null);
                    }
            }

            // At this point we should safely assume we deal with generic types only
            var expectedTypeGenericDefinition = expectedType.IsGenericType ? expectedType.GetGenericTypeDefinition() : null;
            var expectedTypeArguments = expectedType.IsGenericType ? expectedType.GetGenericArguments() : Type.EmptyTypes;
            if (!expectedType.IsInterface)
            {
                do
                {
                    if (type.IsGenericType)
                    {
                        var genericTypeDefinition = type.GetGenericTypeDefinition();
                        if (genericTypeDefinition == expectedTypeGenericDefinition)
                        {
                            runtimeType = _runtimeSubstituteTypesCache.GetOrAdd(cacheKey, k => type);
                            return true;
                        }
                    }
                    type = type.BaseType;
                } while (type != null);
            }
            else
            {
                if (type.IsGenericType)
                {
                    var td = type.GetGenericTypeDefinition();
                    if (td == expectedTypeGenericDefinition && SatisfiesTypeConstraints(type, expectedType, out runtimeType))
                    {
                        _runtimeSubstituteTypesCache.TryAdd(cacheKey, runtimeType);
                        return true;
                    }
                }

                var interfaces = type.GetInterfaces();
                for (int i = 0; i < interfaces.Length; i++)
                {
                    var iface = interfaces[i];
                    if (!iface.IsGenericType) continue;
                    var ifaceDef = iface.GetGenericTypeDefinition();
                    if (ifaceDef != expectedTypeGenericDefinition) continue;
                    var ifaceArgs = iface.GetGenericArguments();
                    if (ifaceArgs.Length != expectedTypeArguments.Length) continue;

                    // Direct equality check is sufficient for generic definitions here
                    if (SatisfiesTypeConstraints(iface, expectedType, out runtimeType))
                    {
                        _runtimeSubstituteTypesCache.TryAdd(cacheKey, runtimeType);
                        return true;
                    }
                }
            }
            _runtimeSubstituteTypesCache.TryAdd(cacheKey, null);
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

            var genericTypeArguments = type.GetGenericArguments();
            var expectedTypeArguments = expectedType.GetGenericArguments();

            if (genericTypeArguments.Length != expectedTypeArguments.Length) return false;

            int l = genericTypeArguments.Length;
            var substitutedArgs = new Type[l];
            for (int i = 0; i < l; i++)
            {
                var typeArg = genericTypeArguments[i];
                var expectedTypeArg = expectedTypeArguments[i];
                if (!expectedTypeArg.IsGenericParameter)
                {
                    // Fast-path: if assignable, avoid heavier IsSubtypeOf
                    if (expectedTypeArg == typeArg || expectedTypeArg.IsAssignableFrom(typeArg))
                    {
                        substitutedArgs[i] = typeArg;
                    }
                    else
                    {
                        if (!typeArg.IsSubtypeOf(expectedTypeArg, out var substitutedArg)) return false;
                        substitutedArgs[i] = substitutedArg;
                    }
                }
                else
                {
                    // Check generic parameter constraints only when necessary
                    var constraints = expectedTypeArg.GetGenericParameterConstraints();
                    for (int c = 0; c < constraints.Length; c++)
                    {
                        var constraint = constraints[c];
                        // Fast-path: if assignable, skip recursive checks
                        if (constraint == typeArg || constraint.IsAssignableFrom(typeArg)) continue;
                        if (!typeArg.IsSubtypeOf(constraint, out var substitutedArg)) return false;
                    }
                    substitutedArgs[i] = typeArg;
                }
            }

            // Construct the resulting closed generic type only once
            var expectedGenericDef = expectedType.GetGenericTypeDefinition();
            constrainedType = expectedGenericDef.MakeGenericType(substitutedArgs);
            return true;
        }
    }
}
