using System;
#if NETCOREAPP || NET5_0_OR_GREATER
using System.Buffers;
#endif
using System.Collections.Concurrent;
using System.Collections.Generic;
#if NET45_OR_GREATER || NETSTANDARD2_0
using System.Reflection;
#endif

namespace TypeLogic.LiskovWingSubstitutions
{
    /// <summary>
    /// Provides extension methods to determine whether a type can be used in place of another
    /// according to the Liskov/Wing Substitution Principle.
    /// </summary>
    /// <remarks>
    /// IMPORTANT: In this API, the term "subtype" (and methods named `IsSubtypeOf`)
    /// refers to the stronger behavioral definition from Liskov/Wing (considering generic variance,
    /// constraints and runtime substitutability), not merely syntactic or structural subtyping.
    /// </remarks>
    public static class TypeExtensions
    {
        internal struct HandlePair : IEquatable<HandlePair>
        {
            public RuntimeTypeHandle A { get; }
            public RuntimeTypeHandle B { get; }

            public HandlePair(RuntimeTypeHandle a, RuntimeTypeHandle b) { A = a; B = b; }

            public bool Equals(HandlePair other) => A.Equals(other.A) && B.Equals(other.B);
            public override bool Equals(object obj) => obj is HandlePair hp && Equals(hp);
            public override int GetHashCode()
            {
#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
                return HashCode.Combine(A, B);
#else
                unchecked
                {
                    int hash = 17;
                    hash = hash * 31 + A.GetHashCode();
                    hash = hash * 31 + B.GetHashCode();
                    return hash;
                }
#endif
            }
        }

        // Capacités initiales optimisées pour réduire les reallocations
        private const int InitialCacheCapacity = 16;
        private const int InitialSmallCacheCapacity = 8;

        internal static readonly ConcurrentDictionary<SubtypeMatch, ConversionInfo> _conversionCache = new ConcurrentDictionary<SubtypeMatch, ConversionInfo>(Environment.ProcessorCount, InitialCacheCapacity);
        private static readonly ConcurrentDictionary<HandlePair, ConversionInfo> _conversionCacheHandles = new ConcurrentDictionary<HandlePair, ConversionInfo>(Environment.ProcessorCount, InitialCacheCapacity);

        private static readonly ConcurrentDictionary<Type, Type> _genericDefCache = new ConcurrentDictionary<Type, Type>(Environment.ProcessorCount, InitialSmallCacheCapacity);
        private static readonly ConcurrentDictionary<Type, Type[]> _genericArgsCache = new ConcurrentDictionary<Type, Type[]>(Environment.ProcessorCount, InitialSmallCacheCapacity);
        private static readonly ConcurrentDictionary<Type, Type[]> _interfacesCache = new ConcurrentDictionary<Type, Type[]>(Environment.ProcessorCount, InitialSmallCacheCapacity);

        private static readonly ConcurrentDictionary<Type, Type[]> _baseGenericDefsCache = new ConcurrentDictionary<Type, Type[]>(Environment.ProcessorCount, InitialSmallCacheCapacity);
        private static readonly ConcurrentDictionary<Type, ConcurrentDictionary<Type, Type[]>> _interfaceMapCache = new ConcurrentDictionary<Type, ConcurrentDictionary<Type, Type[]>>(Environment.ProcessorCount, InitialSmallCacheCapacity);

        private static readonly ConcurrentDictionary<HandlePair, Type> _implementedGenericInterfaceCache = new ConcurrentDictionary<HandlePair, Type>(Environment.ProcessorCount, InitialSmallCacheCapacity);
        private static readonly Type _negativeSentinel = typeof(TypeExtensions);

        private static readonly ConcurrentDictionary<HandlePair, Type> _satisfiesConstraintsCache = new ConcurrentDictionary<HandlePair, Type>(Environment.ProcessorCount, InitialSmallCacheCapacity);
        private static readonly ConcurrentDictionary<Type, Type[]> _genericParamConstraintsCache = new ConcurrentDictionary<Type, Type[]>(Environment.ProcessorCount, InitialSmallCacheCapacity);

        private static readonly ConcurrentDictionary<HandlePair, bool> _genericDefVarianceCache = new ConcurrentDictionary<HandlePair, bool>(Environment.ProcessorCount, InitialSmallCacheCapacity);
        private static readonly ConcurrentDictionary<SubtypeMatch, byte> _inProgress = new ConcurrentDictionary<SubtypeMatch, byte>(Environment.ProcessorCount, InitialSmallCacheCapacity);

        [ThreadStatic] private static Type[] _tlsTempBuffer;
        [ThreadStatic] private static int _tlsDepth;

        public static void ClearCache()
        {
            _conversionCache.Clear();
            _conversionCacheHandles.Clear();
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
            _tlsTempBuffer = null;
            _tlsDepth = 0;
        }

#if NET45_OR_GREATER || NETSTANDARD2_0
        /// <summary>
        /// Determines whether <paramref name="type"/> can be considered a subtype of <paramref name="expectedType"/>.
        /// </summary>
        /// <param name="type">Source type to check.</param>
        /// <param name="expectedType">Target type to check substitutability against.</param>
        /// <returns>True if <paramref name="type"/> is considered a Liskov/Wing-style subtype of <paramref name="expectedType"/>; otherwise false.</returns>
        public static bool IsSubtypeOf(this TypeInfo type, Type expectedType)
        {
            return IsSubtypeOf(type.AsType(), expectedType, out var substitutionType);
        }

        /// <summary>
        /// Determines whether <paramref name="type"/> can be considered a subtype of <paramref name="expectedType"/>,

        /// and returns the runtime type that satisfies the substitutability check when available.
        /// </summary>
        /// <param name="type">Source type to check.</param>
        /// <param name="expectedType">Target type to check substitutability against.</param>
        /// <param name="runtimeType">When this method returns, contains the runtime type that can be used to satisfy <paramref name="expectedType"/>, or null if not applicable.</param>
        /// <returns>True if <paramref name="type"/> is considered a Liskov/Wing-style subtype of <paramref name="expectedType"/>; otherwise false.</returns>
        public static bool IsSubtypeOf(this TypeInfo type, Type expectedType, out Type runtimeType)
        {
            return IsSubtypeOf(type.AsType(), expectedType, out runtimeType);
        }
#endif

        /// <summary>
        /// Determines whether <paramref name="type"/> can be considered a subtype of <paramref name="expectedType"/>.
        /// </summary>
        /// <param name="type">Source type to check.</param>
        /// <param name="expectedType">Target type to check substitutability against.</param>
        /// <returns>True if <paramref name="type"/> is considered a Liskov/Wing-style subtype of <paramref name="expectedType"/>; otherwise false.</returns>
        public static bool IsSubtypeOf(this Type type, Type expectedType)
        {
            return IsSubtypeOf(type, expectedType, out var substitutionType);
        }

        /// <summary>
        /// Determines whether <paramref name="type"/> can be considered a subtype of <paramref name="expectedType"/>,
        /// and returns the runtime type that satisfies the substitutability check when available.
        /// </summary>
        /// <param name="type">Source type to check.</param>
        /// <param name="expectedType">Target type to check substitutability against.</param>
        /// <param name="runtimeType">When this method returns, contains the runtime type that can be used to satisfy <paramref name="expectedType"/>, or null if not applicable.</param>
        /// <returns>True if <paramref name="type"/> is considered a Liskov/Wing-style subtype of <paramref name="expectedType"/>; otherwise false.</returns>
        public static bool IsSubtypeOf(this Type type, Type expectedType, out Type runtimeType)
        {
            runtimeType = null;
            if (type == null || expectedType == null) return false;
            if (type == expectedType)
            {
                runtimeType = type; return true;
            }

            // Prioriser le cache basé sur HandlePair (plus rapide)
            var handleKey = new HandlePair(type.TypeHandle, expectedType.TypeHandle);
            
            if (_conversionCacheHandles.TryGetValue(handleKey, out var cachedEntry))
            {
                if (!cachedEntry.IsConvertible) return false;
                runtimeType = cachedEntry.RuntimeType; 
                return true;
            }

            // Fallback sur le cache legacy SubtypeMatch (pour compatibilité)
            var cacheKey = new SubtypeMatch(type, expectedType);
            if (_conversionCache.TryGetValue(cacheKey, out var cachedLegacy))
            {
                if (!cachedLegacy.IsConvertible) return false;
                runtimeType = cachedLegacy.RuntimeType;
                // Migrer vers le cache HandlePair pour les futurs accès
                _conversionCacheHandles.TryAdd(handleKey, cachedLegacy);
                return true;
            }

            if (type.ContainsGenericParameters && !expectedType.ContainsGenericParameters)
            {
                var neg = ConversionInfo.Negative;
                _conversionCacheHandles.TryAdd(handleKey, neg);
                _conversionCache.TryAdd(cacheKey, neg);
                return false;
            }

            if (expectedType.IsAssignableFrom(type))
            {
                var info = ConversionInfo.Register(cacheKey, expectedType);
                _conversionCacheHandles.TryAdd(handleKey, info);
                _conversionCache.TryAdd(cacheKey, info);
                runtimeType = info.RuntimeType; 
                return true;
            }

            if (!_inProgress.TryAdd(cacheKey, 0))
            {
                var neg = ConversionInfo.Negative;
                _conversionCacheHandles.TryAdd(handleKey, neg);
                _conversionCache.TryAdd(cacheKey, neg);
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
                    if (elemType.IsSubtypeOf(expectedTypeArguments[0], out var substitutedArg))
                    {
                        runtimeType = typeof(IEnumerable<>).MakeGenericType(substitutedArg);
                        var info = ConversionInfo.Register(cacheKey, runtimeType);
                        _conversionCacheHandles.TryAdd(handleKey, info);
                        _conversionCache.TryAdd(cacheKey, info);
                        return true;
                    }
                }
                if (type == typeof(string) && expectedTypeGenericDefinition == typeof(IEnumerable<>))
                {
                    var tArg = expectedTypeArguments.Length > 0 ? expectedTypeArguments[0] : null;
                    if (tArg == typeof(char))
                    {
                        runtimeType = typeof(IEnumerable<char>);
                        var info = ConversionInfo.Register(cacheKey, runtimeType);
                        _conversionCacheHandles.TryAdd(handleKey, info);
                        _conversionCache.TryAdd(cacheKey, info);
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
                            var info = ConversionInfo.Register(cacheKey, type);
                            _conversionCacheHandles.TryAdd(handleKey, info);
                            _conversionCache.TryAdd(cacheKey, info);
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
                        if (typeGenDef == expectedTypeGenericDefinition)
                        {
                            if (TryGetSatisfyingArguments(type, expectedType, out var args))
                            {
                                var constructed = GetGenericDefinitionCached(expectedType).MakeGenericType(args);
                                _satisfiesConstraintsCache.TryAdd(new HandlePair(type.TypeHandle, expectedType.TypeHandle), constructed);
                                var info = ConversionInfo.Register(cacheKey, constructed);
                                _conversionCacheHandles.TryAdd(handleKey, info);
                                _conversionCache.TryAdd(cacheKey, info);
                                runtimeType = constructed; 
                                return true;
                            }
                        }
                    }

                    var implemented = GetImplementedGenericInterfaceCached(type, expectedTypeGenericDefinition, expectedArgCount);
                    if (implemented != null)
                    {
                        var implGenDef = GetGenericDefinitionCached(implemented);
                        if (IsGenericDefinitionVariantOf(implGenDef, expectedTypeGenericDefinition))
                        {
                            if (TryGetSatisfyingArguments(implemented, expectedType, out var args))
                            {
                                var constructed = GetGenericDefinitionCached(expectedType).MakeGenericType(args);
                                _satisfiesConstraintsCache.TryAdd(new HandlePair(implemented.TypeHandle, expectedType.TypeHandle), constructed);
                                var info = ConversionInfo.Register(cacheKey, constructed);
                                _conversionCacheHandles.TryAdd(handleKey, info);
                                _conversionCache.TryAdd(cacheKey, info);
                                runtimeType = constructed; 
                                return true;
                            }
                        }
                    }
                }

                var negInfo = ConversionInfo.Negative;
                _conversionCacheHandles.TryAdd(handleKey, negInfo);
                _conversionCache.TryAdd(cacheKey, negInfo);
                return false;
            }
            finally
            {
                _inProgress.TryRemove(cacheKey, out var _);
            }
        }

        private static bool TryGetSatisfyingArguments(Type type, Type expectedType, out Type[] substitutedArgs)
        {
            substitutedArgs = null;
            var handleKey = new HandlePair(type.TypeHandle, expectedType.TypeHandle);
            if (_satisfiesConstraintsCache.TryGetValue(handleKey, out var cached))
            {
                if (cached == _negativeSentinel) return false;
                // Éviter l'allocation : le cache contient déjà le type construit
                substitutedArgs = GetGenericArgumentsCached(cached);
                return true;
            }

            Type[] genericTypeArguments = GetGenericArgumentsCached(type);
            Type[] expectedTypeArguments = GetGenericArgumentsCached(expectedType);
            if (genericTypeArguments.Length != expectedTypeArguments.Length)
            {
                _satisfiesConstraintsCache.TryAdd(handleKey, _negativeSentinel);
                return false;
            }

            int l = genericTypeArguments.Length;
            _tlsDepth++;
            Type[] temp = null;
            bool rentedFromPool = false;
            
            try
            {
                // Utiliser TLS buffer pour le premier niveau
                if (_tlsDepth == 1)
                {
                    if (_tlsTempBuffer == null || _tlsTempBuffer.Length < l) 
                        _tlsTempBuffer = new Type[l];
                    temp = _tlsTempBuffer;
                }
                else
                {
#if NETCOREAPP || NET5_0_OR_GREATER
                    // Utiliser ArrayPool pour réduire les allocations en récursion
                    temp = ArrayPool<Type>.Shared.Rent(l);
                    rentedFromPool = true;
#else
                    temp = new Type[l];
#endif
                }

                for (int i = 0; i < l; i++)
                {
                    var typeArg = genericTypeArguments[i];
                    var expectedTypeArg = expectedTypeArguments[i];

                    if (!expectedTypeArg.IsGenericParameter)
                    {
                        if (!typeArg.IsSubtypeOf(expectedTypeArg, out var substitutedArg))
                        {
                            _satisfiesConstraintsCache.TryAdd(handleKey, _negativeSentinel);
                            return false;
                        }
                        temp[i] = substitutedArg;
                    }
                    else
                    {
                        var constraints = _genericParamConstraintsCache.GetOrAdd(expectedTypeArg, key => key.GetGenericParameterConstraints());
                        for (int c = 0; c < constraints.Length; c++)
                        {
                            if (!typeArg.IsSubtypeOf(constraints[c], out var _))
                            {
                                _satisfiesConstraintsCache.TryAdd(handleKey, _negativeSentinel);
                                return false;
                            }
                        }
                        temp[i] = typeArg;
                    }
                }

                // Allouer une seule fois pour le résultat final
                substitutedArgs = new Type[l];
                Array.Copy(temp, 0, substitutedArgs, 0, l);
                
                return true;
            }
            finally
            {
                _tlsDepth--;
                
#if NETCOREAPP || NET5_0_OR_GREATER
                // Retourner le buffer au pool si emprunté
                if (rentedFromPool && temp != null)
                {
                    Array.Clear(temp, 0, l); // Nettoyer les références
                    ArrayPool<Type>.Shared.Return(temp);
                }
#endif
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
            var result = defA.IsSubtypeOf(defB);
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
            _tlsDepth++;
            Type[] rented = null;
            bool rentedFromPool = false;
            
            try
            {
                // Utiliser TLS buffer pour le premier niveau
                if (_tlsDepth == 1)
                {
                    if (_tlsTempBuffer == null || _tlsTempBuffer.Length < l) 
                        _tlsTempBuffer = new Type[l];
                    rented = _tlsTempBuffer;
                }
                else
                {
#if NETCOREAPP || NET5_0_OR_GREATER
                    // Utiliser ArrayPool pour réduire les allocations en récursion
                    rented = ArrayPool<Type>.Shared.Rent(l);
                    rentedFromPool = true;
#else
                    rented = new Type[l];
#endif
                }

                for (int i = 0; i < l; i++)
                {
                    var typeArg = genericTypeArguments[i];
                    var expectedTypeArg = expectedTypeArguments[i];

                    if (!expectedTypeArg.IsGenericParameter)
                    {
                        if (!typeArg.IsSubtypeOf(expectedTypeArg, out var substitutedArg))
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
                            if (!typeArg.IsSubtypeOf(constraints[c], out var _))
                            {
                                _satisfiesConstraintsCache.TryAdd(handleKey, _negativeSentinel);
                                return false;
                            }
                        }
                        rented[i] = typeArg;
                    }
                }

                // Créer le type construit à partir du buffer
                var exact = new Type[l];
                Array.Copy(rented, 0, exact, 0, l);
                constrainedType = GetGenericDefinitionCached(expectedType).MakeGenericType(exact);
                _satisfiesConstraintsCache.TryAdd(handleKey, constrainedType);
                
                return true;
            }
            finally
            {
                _tlsDepth--;
                
#if NETCOREAPP || NET5_0_OR_GREATER
                // Retourner le buffer au pool si emprunté
                if (rentedFromPool && rented != null)
                {
                    Array.Clear(rented, 0, l); // Nettoyer les références
                    ArrayPool<Type>.Shared.Return(rented);
                }
#endif
            }
        }
    }
}