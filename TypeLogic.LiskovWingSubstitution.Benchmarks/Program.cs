using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using System;
using System.Reflection;

namespace TypeLogic.LiskovWingSubstitution.Benchmarks
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var config = DefaultConfig.Instance;
            
#if !NETCOREAPP
            config = config.WithOption(ConfigOptions.JoinSummary, true)
                         .WithOption(ConfigOptions.DisableOptimizationsValidator, true);
#endif

            // Use BenchmarkSwitcher to allow running any benchmark class
            var switcher = BenchmarkSwitcher.FromAssembly(Assembly.GetExecutingAssembly());
            switcher.Run(args, config);
        }
    }
}
