using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace Benchmarks
{
    class Program
    {
        static void Main(string[] args) 
            => BenchmarkSwitcher
                .FromAssembly(typeof(Program).Assembly)
                .Run(args, DefaultConfig.Instance
                    .With(MemoryDiagnoser.Default)
                    .With(Job.ShortRun));
    }
}
