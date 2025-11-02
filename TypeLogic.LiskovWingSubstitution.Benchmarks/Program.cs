using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using System;
using System.Linq;

namespace TypeLogic.LiskovWingSubstitution.Benchmarks
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var config = DefaultConfig.Instance;
            
#if !NETCOREAPP
            // Add framework-specific configurations
            config = config.WithOption(ConfigOptions.JoinSummary, true)
                         .WithOption(ConfigOptions.DisableOptimizationsValidator, true);
#endif

            BenchmarkRunner.Run<IsVariantOfBenchmark>(config, args);
        }
    }
}
