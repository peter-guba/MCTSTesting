using System;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using CMS.Benchmark.Config;
using CMS.Benchmark.Exceptions;

namespace CMS.Benchmark
{
    /// <summary>
    /// Class to create and run a set of benchmarks from "Resources/BenchmarkSets".
    /// </summary>
    internal class BenchmarkRunner
    {
        private static Random r = new Random();

        private string basePath = "results";

        /// <summary>
        /// Tries to create and run given benchmark set.
        /// </summary>
        /// <param name="benchmarkSetId">File name of the benchmark set.</param>
        public void Run(string benchmarkSetId)
        {
            string resultsDir = Path.Combine(basePath, benchmarkSetId, DateTime.Now.ToString("yyyy_MM_dd-HH_mm_ss") + '_' + r.Next(100000, 1000000));
            if (!Directory.Exists(resultsDir))
                Directory.CreateDirectory(resultsDir);
            try
            {
                Console.WriteLine("Benchmarking started...");

                var benchmarkSet = BenchmarkFactory.MakeBenchmarkSet(benchmarkSetId);
                foreach (Benchmark benchmark in benchmarkSet)
                {
                    benchmark.Run(resultsDir);
                }

                Console.WriteLine("Benchmarking finished...");
            }
            catch (XmlSchemaValidationException e)
            {
                Console.WriteLine(e.Message);
            }
            catch (XmlException e)
            {
                Console.WriteLine($"Error parsing xml: {e.SourceUri}");
                Console.WriteLine(e.Message);
            }
            catch (ResourceMissingException e)
            {
                Console.WriteLine(e.Message);
                //Console.WriteLine(e.StackTrace);
            }
            catch (InvalidXmlDataException e)
            {
                Console.WriteLine(e.Message);
            }
            catch (InvalidResourceReferenceException e)
            {
                Console.WriteLine("Invalid resource reference");
                Console.WriteLine(e.Message);
            }

            Console.WriteLine("Terminating...");
        }
    }
}
