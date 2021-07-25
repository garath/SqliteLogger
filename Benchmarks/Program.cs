using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace Benchmarks
{

    class Program
    {
        static void Main(string[] args)
        {
            //BenchmarkRunner.Run<LoggerBenchmark>();
            //BenchmarkRunner.Run<MoreComplicatedBenchmark>();

            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
            //BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, new DebugInProcessConfig());
        }
    }
}
