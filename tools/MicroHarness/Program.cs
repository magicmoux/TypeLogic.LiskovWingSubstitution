using System;
using System.Threading;
using TypeLogic.LiskovWingSubstitutions;

class Program
{
    static void Main(string[] args)
    {
        // Prepare some type pairs similar to benchmarks but avoid test-only types
        var pairs = new (Type Source, Type Target)[]
        {
            (typeof(System.Collections.Generic.List<string>), typeof(System.Collections.Generic.IEnumerable<object>)),
            (typeof(System.Collections.Generic.List<string>), typeof(System.Collections.Generic.IEnumerable<string>)),
            (typeof(string[]), typeof(System.Collections.Generic.IEnumerable<object>)),
            (typeof(System.Collections.Generic.List<>), typeof(System.Collections.Generic.IEnumerable<>)),
            (typeof(System.Collections.Generic.List<int>), typeof(System.Collections.Generic.IEnumerable<int>))
        };

        // Force JIT and warm caches
        foreach (var p in pairs)
        {
            p.Source.IsSubtypeOf(p.Target);
        }

        Console.WriteLine("Starting tight loop. Press Ctrl+C to stop.");
        // Tight loop: repeatedly call uncached path
        while (true)
        {
            foreach (var p in pairs)
            {
                TypeExtensions.ClearCache();
                p.Source.IsSubtypeOf(p.Target);
            }
            Thread.Sleep(1);
        }
    }
}
