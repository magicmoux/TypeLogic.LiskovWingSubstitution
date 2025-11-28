using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace TypeLogic.LiskovWingSubstitutions
{
    /// <summary>
    /// Provides extension methods to check type variance relationships according to the Liskov/Wing Substitution Principle.
    /// </summary>
    public static class TypeExtensions
    {
        internal struct HandlePair : IEquatable<HandlePair>
        {
            public RuntimeTypeHandle A { get; }
            public RuntimeTypeHandle B { get; }
            public HandlePair(RuntimeTypeHandle a, RuntimeTypeHandle b) { A = a; B = b; }
            public bool Equals(HandlePair other) => A.Equals(other.A) && B.Equals(other.B);
            public override bool Equals(object obj) => obj is HandlePair hp && Equals(hp);
            public override int GetHashCode() => A.GetHashCode() * 397 ^ B.GetHashCode();
        }

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

        // Precomputed base generic definitions for a type (excluding nulls)
        private static readonly ConcurrentDictionary<Type, Type[]> _baseGenericDefsCache = new ConcurrentDictionary<Type, Type[]>();
        // Interface grouping cache: Type -> (genericDef -> List of closed interface types)
        private static readonly ConcurrentDictionary<Type, ConcurrentDictionary<Type, Type[]>> _interfaceMapCache = new ConcurrentDictionary<Type, ConcurrentDictionary<Type, Type[]>>();

        // Cache to avoid scanning interfaces repeatedly: maps (typeHandle, genericDefinitionHandle) -> implemented interface type or negative sentinel
        private static readonly ConcurrentDictionary<HandlePair, Type> _implementedGenericInterfaceCache = new ConcurrentDictionary<HandlePair, Type>();
        // Negative sentinel to mark absence in cache (cannot store null in ConcurrentDictionary values)
        private static readonly Type _negativeSentinel = typeof(TypeExtensions);

        // Cache for results of SatisfiesTypeConstraints to avoid recomputation on recursive calls (keyed by handles)
        private static readonly ConcurrentDictionary<HandlePair, Type> _satisfiesConstraintsCache = new ConcurrentDictionary<HandlePair, Type>();
        // Cache for generic parameter constraints arrays
        private static readonly ConcurrentDictionary<Type, Type[]> _genericParamConstraintsCache = new ConcurrentDictionary<Type, Type[]>();

        // Cache for generic-definition variance checks (pair of generic definitions) keyed by handles
        private static readonly ConcurrentDictionary<HandlePair, bool> _genericDefVarianceCache = new ConcurrentDictionary<HandlePair, bool>();

        // In-progress marker to prevent re-entrant/exponential recursion
        private static readonly ConcurrentDictionary<VariantTypePair, byte> _inProgress = new ConcurrentDictionary<VariantTypePair, byte>();

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
            _baseGenericDefsCache.Clear();
            _interfaceMapCache.Clear();
            _satisfiesConstraintsCache.Clear();
            _genericParamConstraintsCache.Clear();
            _genericDefVarianceCache.Clear();
            _inProgress.Clear();
        }

#if NET45_OR_GREATER || NETSTANDARD2_0
        public static bool IsVariantOf(this TypeInfo type, Type expectedType)
        {
            return IsVariantOf(type.AsType(), expectedType, out var substitutionType);
        }

        public static bool IsVariantOf(this TypeInfo type, Type expectedType, out Type substitutionType)
        {
            return IsVariantOf(type.AsType(), expectedType, out substitutionType);
        }
#endif

        public static bool IsVariantOf(this Type type, Type expectedType)
        {
            return IsVariantOf(type, expectedType, out var substitutionType);
        }

        public static bool IsVariantOf(this Type type, Type expectedType, out Type runtimeType)
        {
            runtimeType = null;

            if (type == null || expectedType == null) return false;
            if (type == expectedType)
            {
                runtimeType = type;
                return true;
            }

            var cacheKey = new VariantTypePair(type, expectedType);

            if (_conversionCache.TryGetValue(cacheKey, out var cached))
            {
                if (!cached.IsConvertible) return false;
                runtimeType = cached.RuntimeType;
                return true;
            }

            if (type.ContainsGenericParameters && !expectedType.ContainsGenericParameters)
            {
                _conversionCache.TryAdd(cacheKey, ConversionInfo.Negative);
                return false;
            }

            if (expectedType.IsAssignableFrom(type))
            {
                var info = _conversionCache.GetOrAdd(cacheKey, k => ConversionInfo.Register(k, expectedType));
                runtimeType = info.RuntimeType;
                return true;
            }

            // prevent re-entrant evaluation on the same pair
            if (!_inProgress.TryAdd(cacheKey, 0))
            {
                _conversionCache.TryAdd(cacheKey, ConversionInfo.Negative);
                return false;
            }

            try
            {
                var expectedTypeGenericDefinition = GetGenericDefinitionCached(expectedType);
                var expectedTypeArguments = GetGenericArgumentsCached(expectedType);
                int expectedArgCount = expectedTypeArguments.Length;

                // Fast-paths
                if (type.IsArray && expectedTypeGenericDefinition == typeof(IEnumerable<>))
                {
                    var elemType = type.GetElementType();
                    if (elemType.IsVariantOf(expectedTypeArguments[0], out var substitutedArg))
                    {
                        runtimeType = typeof(IEnumerable<>).MakeGenericType(substitutedArg);
                        _conversionCache.TryAdd(cacheKey, ConversionInfo.Register(cacheKey, runtimeType));
                        return true;
                    }
                }
                if (type == typeof(string) && expectedTypeGenericDefinition == typeof(IEnumerable<>))
                {
                    var tArg = expectedTypeArguments.Length > 0 ? expectedTypeArguments[0] : null;
                    if (tArg == typeof(char))
                    {
                        runtimeType = typeof(IEnumerable<char>);
                        _conversionCache.TryAdd(cacheKey, ConversionInfo.Register(cacheKey, runtimeType));
                        return true;
                    }
                }

                if (!expectedType.IsInterface)
                {
                    var baseGens = GetBaseGenericDefsCached(type);
                    for (int i = 0; i < baseGens.Length; i++)
                    {
                        if (baseGens[i] == expectedTypeGenericDefinition)
                        {
                            var info = _conversionCache.GetOrAdd(cacheKey, k => ConversionInfo.Register(k, type));
                            runtimeType = info.RuntimeType;
                            return true;
                        }
                    }
                }
                else
                {
                    if (type.IsGenericType)
                    {
                        var typeGenDef = GetGenericDefinitionCached(type);
                        if (typeGenDef == expectedTypeGenericDefinition && SatisfiesTypeConstraints(type, expectedType, out runtimeType))
                        {
                            _conversionCache.TryAdd(cacheKey, ConversionInfo.Register(cacheKey, runtimeType));
                            return true;
                        }
                    }

                    var implemented = GetImplementedGenericInterfaceCached(type, expectedTypeGenericDefinition, expectedArgCount);
                    if (implemented != null)
                    {
                        var implGenDef = GetGenericDefinitionCached(implemented);
                        if (IsGenericDefinitionVariantOf(implGenDef, expectedTypeGenericDefinition) && SatisfiesTypeConstraints(implemented, expectedType, out runtimeType))
                        {
                            _conversionCache.TryAdd(cacheKey, ConversionInfo.Register(cacheKey, runtimeType));
                            return true;
                        }
                    }
                }

                _conversionCache.TryAdd(cacheKey, ConversionInfo.Negative);
                return false;
            }
            finally
            {
                _inProgress.TryRemove(cacheKey, out var _);
            }
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

        private static Type[] GetBaseGenericDefsCached(Type t)
        {
            return _baseGenericDefsCache.GetOrAdd(t, key =>
            {
                var list = new List<Type>();
                var cur = key.BaseType;
                while (cur != null)
                {
                    if (cur.IsGenericType) list.Add(cur.GetGenericTypeDefinition());
                    cur = cur.BaseType;
                }
                return list.ToArray();
            });
        }

        private static ConcurrentDictionary<Type, Type[]> GetInterfaceMapCached(Type t)
        {
            return _interfaceMapCache.GetOrAdd(t, key =>
            {
                var map = new ConcurrentDictionary<Type, Type[]>();
                var ifaces = GetInterfacesCached(key);
                for (int i = 0; i < ifaces.Length; i++)
                {
                    var iface = ifaces[i];
                    if (!iface.IsGenericType) continue;
                    var genDef = iface.GetGenericTypeDefinition();
                    map.AddOrUpdate(genDef, new[] { iface }, (k, v) => { var arr = new Type[v.Length + 1]; Array.Copy(v, arr, v.Length); arr[v.Length] = iface; return arr; });
                }
                return map;
            });
        }

        private static Type GetImplementedGenericInterfaceCached(Type type, Type expectedGenericDefinition, int expectedArgCount)
        {
            if (expectedGenericDefinition == null) return null;
            var key = new HandlePair(type.TypeHandle, expectedGenericDefinition.TypeHandle);
            if (_implementedGenericInterfaceCache.TryGetValue(key, out var cached))
            {
                return cached == _negativeSentinel ? null : cached;
            }

            // check the type itself
            if (type.IsGenericType)
            {
                var def = GetGenericDefinitionCached(type);
                if (def == expectedGenericDefinition && GetGenericArgumentsCached(type).Length == expectedArgCount)
                {
                    _implementedGenericInterfaceCache.TryAdd(key, type);
                    return type;
                }
            }

            var map = GetInterfaceMapCached(type);
            if (map.TryGetValue(expectedGenericDefinition, out var arr))
            {
                for (int i = 0; i < arr.Length; i++)
                {
                    var iface = arr[i];
                    if (GetGenericArgumentsCached(iface).Length != expectedArgCount) continue;
                    _implementedGenericInterfaceCache.TryAdd(key, iface);
                    return iface;
                }
            }

            _implementedGenericInterfaceCache.TryAdd(key, _negativeSentinel);
            return null;
        }

        private static bool IsGenericDefinitionVariantOf(Type defA, Type defB)
        {
            if (defA == defB) return true;
            var key = new HandlePair(defA.TypeHandle, defB.TypeHandle);
            if (_genericDefVarianceCache.TryGetValue(key, out var cached)) return cached;
            var result = defA.IsVariantOf(defB);
            _genericDefVarianceCache.TryAdd(key, result);
            return result;
        }

        private static bool SatisfiesTypeConstraints(Type type, Type expectedType, out Type constrainedType)
        {
            constrainedType = null;
            var handleKey = new HandlePair(type.TypeHandle, expectedType.TypeHandle);
            if (_satisfiesConstraintsCache.TryGetValue(handleKey, out var cachedResult))
            {
                if (cachedResult == _negativeSentinel) { constrainedType = null; return false; }
                constrainedType = cachedResult; return true;
            }

            Type[] genericTypeArguments = GetGenericArgumentsCached(type);
            Type[] expectedTypeArguments = GetGenericArgumentsCached(expectedType);

            if (genericTypeArguments.Length != expectedTypeArguments.Length)
            {
                _satisfiesConstraintsCache.TryAdd(handleKey, _negativeSentinel);
                return false;
            }

            int l = genericTypeArguments.Length;
            var rented = new Type[l];
            try
            {
                for (int i = 0; i < l; i++)
                {
                    var typeArg = genericTypeArguments[i];
                    var expectedTypeArg = expectedTypeArguments[i];

                    if (!expectedTypeArg.IsGenericParameter)
                    {
                        if (!typeArg.IsVariantOf(expectedTypeArg, out var substitutedArg))
                        {
                            _satisfiesConstraintsCache.TryAdd(handleKey, _negativeSentinel);
                            return false;
                        }
                        rented[i] = substitutedArg;
                    }
                    else
                    {
                        var constraints = _genericParamConstraintsCache.GetOrAdd(expectedTypeArg, key => key.GetGenericParameterConstraints());
                        for (int c = 0; c < constraints.Length; c++)
                        {
                            if (!typeArg.IsVariantOf(constraints[c], out var _))
                            {
                                _satisfiesConstraintsCache.TryAdd(handleKey, _negativeSentinel);
                                return false;
                            }
                        }
                        rented[i] = typeArg;
                    }
                }

                var exact = new Type[l];
                Array.Copy(rented, exact, l);
                constrainedType = GetGenericDefinitionCached(expectedType).MakeGenericType(exact);
                _satisfiesConstraintsCache.TryAdd(handleKey, constrainedType);
                return true;
            }
            finally
            {
                // nothing to return when using new allocations
            }
        }
    }
}