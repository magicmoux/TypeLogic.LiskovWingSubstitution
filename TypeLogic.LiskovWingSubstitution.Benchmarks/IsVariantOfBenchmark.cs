using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TypeLogic.LiskovWingSubstitutions;

namespace TypeLogic.LiskovWingSubstitution.Benchmarks
{
    [MemoryDiagnoser]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    [Config(typeof(Config))]
    public class IsVariantOfBenchmark
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                AddDiagnoser(MemoryDiagnoser.Default);
                // Add jobs for each framework
                AddJob(Job.Default.WithRuntime(ClrRuntime.Net462)
                    .WithWarmupCount(3)
                    .WithIterationCount(15)
                    .WithId(".NET 4.6.2"));

                AddJob(Job.Default.WithRuntime(ClrRuntime.Net47)
                    .WithWarmupCount(3)
                    .WithIterationCount(15)
                    .WithId(".NET 4.7"));

                AddJob(Job.Default.WithRuntime(ClrRuntime.Net48)
                    .WithWarmupCount(3)
                    .WithIterationCount(15)
                    .WithId(".NET 4.8"));

                AddJob(Job.Default.WithRuntime(CoreRuntime.Core80)
                    .WithWarmupCount(3)
                    .WithIterationCount(15)
                    .WithId(".NET 8.0"));
            }
        }

        // Test data for different scenarios
        private static readonly Type ListString = typeof(List<string>);
        private static readonly Type IEnumerableObject = typeof(IEnumerable<object>);
        private static readonly Type IEnumerableString = typeof(IEnumerable<string>);
        private static readonly Type StringArray = typeof(string[]);
        private static readonly Type GenericList = typeof(List<>);
        private static readonly Type GenericIEnumerable = typeof(IEnumerable<>);
        
        // Instance test data
        private List<string> listInstance;
        private string[] arrayInstance;
        private IEnumerable<string> enumerableInstance;

        [ParamsSource(nameof(TypePairs))]
        public TypePair Types { get; set; }

        public IEnumerable<TypePair> TypePairs()
        {
            // Direct interface implementation (fast path)
            yield return new TypePair(ListString, IEnumerableString, "List<string> -> IEnumerable<string>");
            
            // Covariant interface conversion
            yield return new TypePair(ListString, IEnumerableObject, "List<string> -> IEnumerable<object>");
            
            // Array to interface (special case)
            yield return new TypePair(StringArray, IEnumerableObject, "string[] -> IEnumerable<object>");
            
            // Generic definition to closed type (complex case)
            yield return new TypePair(GenericList, IEnumerableString, "List<> -> IEnumerable<string>");
            
            // Generic to generic (complex case)
            yield return new TypePair(GenericList, GenericIEnumerable, "List<> -> IEnumerable<>");
        }

        [GlobalSetup]
        public void Setup()
        {
            // Initialize test instances
            listInstance = new List<string> { "test" };
            arrayInstance = new[] { "test" };
            enumerableInstance = listInstance;

            // Warm up type cache with some unrelated types to simulate real-world scenario
            typeof(Dictionary<,>).IsVariantOf(typeof(IEnumerable<>));
            typeof(List<int>).IsVariantOf(typeof(IEnumerable<int>));
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            // Clear type cache to ensure consistent results between runs
            TypeExtensions.ClearCache();
        }

        [Benchmark(Baseline = true, Description = "Uncached")]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool IsVariantOfUncached()
        {
            TypeExtensions.ClearCache(); // Force uncached path
            return Types.Source.IsVariantOf(Types.Target);
        }

        [Benchmark(Description = "Cached")]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool IsVariantOfCached() => Types.Source.IsVariantOf(Types.Target);

        [Benchmark(Description = "List Instance")]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool IsInstanceOfWithList() => listInstance.IsInstanceOf<List<string>, IEnumerable<object>>();

        [Benchmark(Description = "Array Instance")]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool IsInstanceOfWithArray() => arrayInstance.IsInstanceOf<string[], IEnumerable<object>>();

#if !NETCOREAPP // Framework-specific benchmarks
        [Benchmark(Description = "Framework Only - Reflection Cache")]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool FrameworkReflectionCache()
        {
            // Test reflection caching behavior in .NET Framework
            return Types.Source.GetInterfaces().Length > 0 && 
                   Types.Source.IsVariantOf(Types.Target);
        }
#endif
    }

    public class TypePair
    {
        public Type Source { get; }
        public Type Target { get; }
        public string Description { get; }

        public TypePair(Type source, Type target, string description)
        {
            Source = source;
            Target = target;
            Description = description;
        }

        public override string ToString() => Description;
    }
}