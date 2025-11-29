using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TypeLogic.LiskovWingSubstitutions;
using Microsoft.VSDiagnostics;

namespace TypeLogic.LiskovWingSubstitution.Benchmarks
{
    [MemoryDiagnoser]
    [CPUUsageDiagnoser]
    public class CacheLookupOptimizationBenchmark
    {
        private Type listString;
        private Type ienumerableString;
        private Type ienumerableObject;
        private Type stringArray;
        private Type genericList;
        private Type genericIEnumerable;
        
        [GlobalSetup]
        public void Setup()
        {
            listString = typeof(List<string>);
            ienumerableString = typeof(IEnumerable<string>);
            ienumerableObject = typeof(IEnumerable<object>);
            stringArray = typeof(string[]);
            genericList = typeof(List<>);
            genericIEnumerable = typeof(IEnumerable<>);
            
            listString.IsSubtypeOf(ienumerableString);
            listString.IsSubtypeOf(ienumerableObject);
            stringArray.IsSubtypeOf(ienumerableObject);
        }

        [Benchmark(Baseline = true, Description = "Uncached - Simple")]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool Cache_Uncached_Simple()
        {
            TypeExtensions.ClearCache();
            return listString.IsSubtypeOf(ienumerableString);
        }

        [Benchmark(Description = "Cached - Simple")]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool Cache_Cached_Simple()
        {
            return listString.IsSubtypeOf(ienumerableString);
        }

        [Benchmark(Description = "Cached - Covariant")]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool Cache_Cached_Covariant()
        {
            return listString.IsSubtypeOf(ienumerableObject);
        }

        [Benchmark(Description = "Cached - Array")]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool Cache_Cached_Array()
        {
            return stringArray.IsSubtypeOf(ienumerableObject);
        }

        [Benchmark(Description = "Mixed - Sequential Lookups")]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool Mixed_SequentialLookups()
        {
            bool r1 = listString.IsSubtypeOf(ienumerableString);
            bool r2 = listString.IsSubtypeOf(ienumerableObject);
            bool r3 = stringArray.IsSubtypeOf(ienumerableObject);
            return r1 && r2 && r3;
        }

        [Benchmark(Description = "Generic Definition Lookup")]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool GenericDefinition_Lookup()
        {
            return genericList.IsSubtypeOf(genericIEnumerable);
        }
    }
}
