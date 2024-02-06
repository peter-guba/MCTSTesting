using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading.Tasks;

namespace data_cruncher
{
    internal class SecondaryDataCruncher
    {
        // Options passed to the StreamReaders.
        private static FileStreamOptions fso = new FileStreamOptions();

        private const int roundIndex = 0;
        private const int depthIndex = 1;
        private const int timeIndex = 2;

        public static void CrunchSecondaryData(string[] args)
        {
            // A regex that will be passed to the Directory.GetFiles function in order to filter the files.
            string regex = "";
            if (args.Length > 2)
            {
                regex = args[2];
            }

            Console.WriteLine("Beginning secondary data crunching.");
            Console.WriteLine();

            if (!Directory.Exists(Cruncher.outputPath + "secondary data"))
            {
                Directory.CreateDirectory(Cruncher.outputPath + "secondary data");
                Console.WriteLine("Secondary data folder created.");
                Console.WriteLine();
            }

            // Create a csv table for the given regex with the mean and standard error values for
            // iteration time, tree depth and round count.
            
            Console.WriteLine($"Processing regex {regex}.");

            // Make sure that other programs can read the files that this program is working with.
            fso.Access = FileAccess.Read;
            fso.Share = FileShare.Read;
            fso.Mode = FileMode.Open;

            using (StreamWriter sw = new StreamWriter(Cruncher.outputPath + $"secondary data/td{regex}.csv"))
            {
                sw.WriteLine(";timeMean;timeConfBound;depthMean;depthConfBound;roundCountMean;roundCountConfBound");

                List<string> folders = new List<string>(Directory.GetDirectories(Cruncher.sourcePath + "/time_depth_data"));
                folders.Sort();

                foreach (string folder in folders)
                {
                    sw.Write($"{folder.Substring(folder.LastIndexOf('/') + 1)};");

                    string fileRegex = "*_vs_*" + regex + "*.txt";

                    Console.WriteLine(" Computing means.");
                    List<double> means = GetMeans(folder, fileRegex, new List<int>() { 0, 1, 2 });
                    Console.WriteLine(" Computing confidence bounds.");
                    List<double> bounds = GetConfidenceBounds(folder, fileRegex, means, new List<int>() { 0, 1, 2 });

                    sw.WriteLine($"{means[timeIndex]};{bounds[timeIndex]};{means[depthIndex]};{bounds[depthIndex]};{means[roundIndex]};{bounds[roundIndex]}");
                }
            }

            Console.WriteLine();          
        }

        /// <summary>
        /// Computes the mean value of values find in files that match the given regex.
        /// </summary>
        /// <param name="folder"> The folder in which the files can be found. </param>
        /// <param name="regex"> A regex that is used to filter the files. </param>
        /// <param name="indices"> The indices of the values in the data files that the method is supposed to
        /// take into account. </param>
        private static List<double> GetMeans(string folder, string regex, List<int> indices)
        {
            List<ulong> sums = new List<ulong>();
            List<ulong> sampleCounts = new List<ulong>();

            foreach (int _ in indices)
            {
                sums.Add(0);
                sampleCounts.Add(0);
            }

            foreach (string file in Directory.GetFiles(folder, regex, SearchOption.AllDirectories))
            {
                int maxPlayouts = int.Parse(file.Split('_')[file.Split('_').Length - 4]);

                using (StreamReader sr = new StreamReader(file, fso))
                {
                    while (sr.Peek() != -1)
                    {
                        string line = sr.ReadLine();

                        try
                        {
                            int numOfPlayouts = Int32.Parse(line.Split(',')[0]);

                            for (int i = 0; i < indices.Count; ++i)
                            {
                                // If index equals zero, it means that the computed metric is round count, which
                                // is computed by counting the number of times the maximum number of playouts
                                // was reached.
                                if (indices[i] == roundIndex)
                                {
                                    if (numOfPlayouts == maxPlayouts)
                                    {
                                        ++(sums[i]);
                                    }
                                }
                                // If index equals 1, the selected value is maximum tree depth, where sample count
                                // is incremented every time the maximum number of playouts is reached.
                                else if (indices[i] == depthIndex)
                                {
                                    if (numOfPlayouts == maxPlayouts)
                                    {
                                        sums[i] += ulong.Parse(line.Split(',')[indices[i]]);
                                        ++(sampleCounts[i]);
                                    }
                                }
                                // If index equals two, it means that the selected value is the time measurement,
                                // where sampleCount is incremented every line.
                                else
                                {
                                    sums[i] += (ulong) double.Parse(line.Split(',')[indices[i]]);
                                    ++(sampleCounts[i]);
                                }
                            }
                        }
                        catch (System.FormatException)
                        {
                            Console.WriteLine("Bad line: " + line);
                            Console.WriteLine("Bad file: " + file);
                            throw new Exception("you're givin' me a bad signal");
                        }
                    }
                }

                if (indices.Contains(0))
                {
                    ++(sampleCounts[indices.IndexOf(0)]);
                }
            }

            List<double> results = new List<double>();

            for (int i = 0; i < sums.Count; ++i)
            {
                results.Add(sums[i] / (double)sampleCounts[i]);
            }

            return results;
        }

        /// <summary>
        /// Computes the standard error of values find in files that match the given regex.
        /// </summary>
        /// <param name="folder"> The folder in which the files can be found. </param>
        /// <param name="regex"> A regex that is used to filter the files. </param>
        /// <param name="means"> The means of the variables for which the cofidence bounds are supposed to be
        /// computed. </param>
        /// <param name="indices"> The indices of the values in the data files that the method is supposed to
        /// take into account. </param>
        private static List<double> GetConfidenceBounds(string folder, string regex, List<double> means, List<int> indices)
        {
            List<double> sums = new List<double>();
            List<double> sampleCounts = new List<double>();
            double intermediate = 0.0;

            foreach (int _ in indices)
            {
                sums.Add(0);
                sampleCounts.Add(0);
            }

            foreach (string file in Directory.GetFiles(folder, regex, SearchOption.AllDirectories))
            {
                int maxPlayouts = int.Parse(file.Split('_')[file.Split('_').Length - 4]);

                using (StreamReader sr = new StreamReader(file, fso))
                {
                    while (sr.Peek() != -1)
                    {
                        string line = sr.ReadLine();
                        int numOfPlayouts = Int32.Parse(line.Split(',')[0]);

                        for (int i = 0; i < indices.Count; ++i)
                        {
                            // If index equals zero, the number of rounds must be computed (as the number of times
                            // the maximum number of playouts is reached in a file).
                            if (indices[i] == roundIndex)
                            {
                                if (numOfPlayouts == maxPlayouts)
                                {
                                    ++intermediate;
                                }
                            }
                            // If index equals one the selected value is maximum tree depth where the sampleCount is
                            // incremented every time the maximum number of playouts is reached.
                            else if (indices[i] == depthIndex)
                            {
                                if (numOfPlayouts == maxPlayouts)
                                {
                                    sums[i] += Math.Pow(ulong.Parse(line.Split(',')[indices[i]]) - means[indices[i]], 2);
                                    ++(sampleCounts[i]);
                                }
                            }
                            // Otherwise the index equals two, which means that the selected value is the time measurement
                            // and sampleCount is incremented on every line.
                            else
                            {
                                sums[i] += Math.Pow(double.Parse(line.Split(',')[indices[i]]) - means[indices[i]], 2);
                                ++(sampleCounts[i]);
                            }
                        }
                    }
                }

                if (indices.Contains(0))
                {
                    sums[indices.IndexOf(0)] += Math.Pow(intermediate - means[indices.IndexOf(0)], 2);
                    ++(sampleCounts[indices.IndexOf(0)]);
                    intermediate = 0;
                }
            }

            List<double> results = new List<double>();

            for (int i = 0; i < sums.Count; ++i)
            {
                results.Add(Cruncher.zValue * (Math.Sqrt(sums[i] / (sampleCounts[i] - 1)) / Math.Sqrt(sampleCounts[i])));
            }

            return results;
        }
    }
}
