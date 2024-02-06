using System.Globalization;
using System.Threading;

namespace CMS.Benchmark
{
    internal class Program
    {
        /// <summary>
        /// Expects a name of a benchmark set as the first argument.
        /// </summary>
        public static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            //string benchmarkSet = args.Length == 1 ? args[0] : "(00+03)vs(00+03)";
            string benchmarkSet = args.Length == 1 ? args[0] : "TestBenchSet";
            //string benchmarkSet = args.Length == 1 ? args[0] : "DefaultPGSBenchSet";
            
            var br = new BenchmarkRunner();
            br.Run(benchmarkSet);
        }
    }
}
