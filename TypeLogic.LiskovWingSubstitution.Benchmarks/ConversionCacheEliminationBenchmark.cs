using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TypeLogic.LiskovWingSubstitutions;
using Microsoft.VSDiagnostics;

namespace TypeLogic.LiskovWingSubstitution.Benchmarks
{
    [CPUUsageDiagnoser]
    public class ConversionCacheEliminationBenchmark
    {
        private Type listString;
        private Type ienumerableString;
        private Type ienumerableObject;
        private Type stringArray;
        private Type listInt;
        private Type ienumerableInt;
        private Type dictionaryStringInt;
        private Type idictionaryStringInt;
        [GlobalSetup]
        public void Setup()
        {
            listString = typeof(List<string>);
            ienumerableString = typeof(IEnumerable<string>);
            ienumerableObject = typeof(IEnumerable<object>);
            stringArray = typeof(string[]);
            listInt = typeof(List<int>);
            ienumerableInt = typeof(IEnumerable<int>);
            dictionaryStringInt = typeof(Dictionary<string, int>);
            idictionaryStringInt = typeof(IDictionary<string, int>);
            listString.IsSubtypeOf(ienumerableString);
            listString.IsSubtypeOf(ienumerableObject);
            stringArray.IsSubtypeOf(ienumerableObject);
            listInt.IsSubtypeOf(ienumerableInt);
            dictionaryStringInt.IsSubtypeOf(idictionaryStringInt);
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
            bool r4 = listInt.IsSubtypeOf(ienumerableInt);
            return r1 && r2 && r3 && r4;
        }

        [Benchmark(Description = "Cached - Dictionary Complex")]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool Cache_Cached_DictionaryComplex()
        {
            return dictionaryStringInt.IsSubtypeOf(idictionaryStringInt);
        }
    }
}