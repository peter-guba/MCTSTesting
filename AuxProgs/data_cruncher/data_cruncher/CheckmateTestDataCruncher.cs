using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace data_cruncher
{
    internal class CheckmateTestDataCruncher
    {
        /// <summary>
        /// A nested dictionary into which the data is loaded. The keys in the first layer are
        /// algorithm ids. In the second layer, they are test ids. The third layer is a list -
        /// each element of that lists represents data for a single run of a test. The keys
        /// in that layer are numbers of times the correct move was discovered and the values are
        /// a numbers of moves after which the best move was correctly identified the given number
        /// of times.
        /// </summary>
        private static Dictionary<string, Dictionary<string, List<Dictionary<int, int>>>> data = new Dictionary<string, Dictionary<string, List<Dictionary<int, int>>>>();

        public static void CrunchCheckmateTestData(string[] args)
        {
            if (!Directory.Exists(Cruncher.outputPath + "checkmate data"))
            {
                Directory.CreateDirectory(Cruncher.outputPath + "checkmate data");
                Console.WriteLine("Checkmate data folder created.");
                Console.WriteLine();
            }

            Console.WriteLine("Beginning checkmate data crunching.");
            Console.WriteLine();

            // Go through all the folders and load the contained data.
            foreach (string folder in Directory.GetDirectories(Cruncher.sourcePath + "\\checkmate_tests"))
            {
                foreach (string file in Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories))
                {
                    Console.WriteLine($"    Loading file {file}.");

                    using (StreamReader sr = new StreamReader(file))
                    {
                        string line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            string[] fields = line.Split(';');

                            List<Dictionary<int, int>> counts = new List<Dictionary<int, int>>();
                            for (int fieldsIndex = 1; fieldsIndex < fields.Length - 1; ++fieldsIndex)
                            {
                                int maxKey = 0;

                                counts.Add(new Dictionary<int, int>());
                                string[] moveCounts = fields[fieldsIndex].Split(',');
                                for (int moveCountsIndex = 0; moveCountsIndex < moveCounts.Length; ++moveCountsIndex)
                                {
                                    if (int.TryParse(moveCounts[moveCountsIndex], out _))
                                    {
                                        for (int key = maxKey + 10; key <= 100; key += 10)
                                        {
                                            counts[fieldsIndex - 1][key] = -1;
                                        }
                                    }
                                    else
                                    {
                                        int key = int.Parse(moveCounts[moveCountsIndex].Split(": ")[0]);
                                        int value = int.Parse(moveCounts[moveCountsIndex].Split(": ")[1]);

                                        if (!counts[fieldsIndex - 1].ContainsKey(key))
                                        {
                                            counts[fieldsIndex - 1][key] = value;
                                        }

                                        if (key > maxKey)
                                        {
                                            maxKey = key;
                                        }
                                    }
                                }
                            }

                            string algorithmName = folder.Substring(folder.LastIndexOf('\\') + 1);
                            if (!data.ContainsKey(algorithmName))
                            {
                                data.Add(algorithmName, new Dictionary<string, List<Dictionary<int, int>>>());
                            }

                            if (!data[algorithmName].ContainsKey(fields[0]))
                            {
                                data[algorithmName].Add(fields[0], new List<Dictionary<int, int>>());
                            }

                            data[algorithmName][fields[0]].AddRange(counts);
                        }
                    }
                }
            }

            for (int numOfMoves = 1; numOfMoves <= 7; ++numOfMoves)
            {
                List<string> tests = new List<string>();
                for (int testIndex = 1; testIndex <= 100; ++testIndex)
                {
                    tests.Add(numOfMoves.ToString() + '.' + testIndex.ToString());
                }
                MakeCSV(tests, "checkmate_tests_" + numOfMoves.ToString());
                MakeVarianceCSV(tests, "checkmate_tests_vars_" + numOfMoves.ToString());
            }

            MakeCSV(new List<string>() { "1", "2", "3", "4", "5", "6", "7" }, "checkmate_tests_all");
        }

        /// <summary>
        /// Creates a csv file which contains the average numbers of moves for the given tests.
        /// </summary>
        /// <param name="testsToPrint"> Ids of the tests that should be included in the output.
        /// Partial ids, such as "1." can be included, in which case all tests that start with
        /// the given string are taken into account. </param>
        /// <param name="fileName"> The name of the output file. </param>
        private static void MakeCSV(List<string> testsToPrint, string fileName)
        {
            using (StreamWriter sw = new StreamWriter(Cruncher.outputPath + "checkmate data/" + fileName + ".csv"))
            {
                // Print out the first row which contains all the given test ids.
                sw.Write(";");
                foreach (string testID in testsToPrint)
                {
                    sw.Write(testID + ";");
                }
                sw.WriteLine();

                // Go through the loaded data, compute the averages and print them out.
                foreach (string mcts in data.Keys)
                {
                    sw.Write(mcts + ";");

                    foreach (string testIDToPrint in testsToPrint)
                    {
                        List<double> sums = new List<double>() { 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0 };
                        List<int> counts = new List<int>() { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

                        foreach (string testIDToProcess in data[mcts].Keys)
                        {
                            if ((testIDToPrint.Contains('.') && testIDToProcess == testIDToPrint) ||
                                (!testIDToPrint.Contains('.') && testIDToProcess.StartsWith(testIDToPrint)))
                            {
                                foreach (var elem in data[mcts][testIDToProcess])
                                {
                                    for (int numOfCorrectPicks = 10; numOfCorrectPicks <= 100; numOfCorrectPicks += 10)
                                    {
                                        if (elem[numOfCorrectPicks] != -1)
                                        {
                                            sums[(numOfCorrectPicks / 10) - 1] += elem[numOfCorrectPicks];
                                            ++counts[(numOfCorrectPicks / 10) - 1];
                                        }
                                    }
                                }
                            }
                        }

                        if (counts[0] == 0)
                        {
                            sw.Write("10: -1 (0)");
                        }
                        else
                        {
                            sw.Write("10: " + (sums[0] / counts[0]) + $" ({counts[0]})");
                        }

                        for (int sumsIndex = 1; sumsIndex < sums.Count; ++sumsIndex)
                        {
                            if (counts[sumsIndex] == 0)
                            {
                                sw.Write($", {(sumsIndex + 1) * 10}: -1" + $" ({counts[sumsIndex]})");
                            }
                            else
                            {
                                sw.Write($", {(sumsIndex + 1) * 10}: " + (sums[sumsIndex] / counts[sumsIndex]) + $" ({counts[sumsIndex]})");
                            }
                        }
                        sw.Write(";");
                    }

                    sw.WriteLine();
                }
            }
        }

        private static void MakeVarianceCSV(List<string> testsToPrint, string fileName)
        {
            using (StreamWriter sw = new StreamWriter(Cruncher.outputPath + "checkmate data/" + fileName + ".csv"))
            {
                // Print out the first row which contains all the given test ids.
                sw.Write(";");
                foreach (string testID in testsToPrint)
                {
                    sw.Write(testID + ";");
                }
                sw.WriteLine();

                // Go through the loaded data, compute the averages and print them out.
                foreach (string mcts in data.Keys)
                {
                    sw.Write(mcts + ";");

                    foreach (string testIDToPrint in testsToPrint)
                    {
                        List<double> sums = new List<double>() { 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0 };
                        List<double> vars = new List<double>() { 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0 };

                        foreach (string testIDToProcess in data[mcts].Keys)
                        {
                            if (testIDToProcess == testIDToPrint)
                            {
                                for (int numOfCorrectPicks = 10; numOfCorrectPicks <= 100; numOfCorrectPicks += 10)
                                {
                                    foreach (var elem in data[mcts][testIDToProcess])
                                    {
                                        if (elem[numOfCorrectPicks] == -1)
                                        {
                                            sums[(numOfCorrectPicks / 10) - 1] = -1;
                                            break;
                                        }
                                        else
                                        {
                                            sums[(numOfCorrectPicks / 10) - 1] += elem[numOfCorrectPicks];
                                        }
                                    }
                                }

                                for (int numOfCorrectPicks = 10; numOfCorrectPicks <= 100; numOfCorrectPicks += 10)
                                {
                                    if (sums[(numOfCorrectPicks / 10) - 1] == -1)
                                    {
                                        vars[(numOfCorrectPicks / 10) - 1] = -1;
                                    }
                                    else
                                    {
                                        foreach (var elem in data[mcts][testIDToProcess])
                                        {
                                            vars[(numOfCorrectPicks / 10) - 1] += Math.Pow(elem[numOfCorrectPicks] - sums[(numOfCorrectPicks / 10) - 1] / 10, 2);
                                        }

                                        vars[(numOfCorrectPicks / 10) - 1] = Math.Sqrt(vars[(numOfCorrectPicks / 10) - 1] / 9);
                                    }
                                }

                                break;
                            }
                        }

                        if (vars[0] == -1)
                        {
                            sw.Write("10: -1");
                        }
                        else
                        {
                            sw.Write("10: " + (vars[0]));
                        }

                        for (int varsIndex = 1; varsIndex < sums.Count; ++varsIndex)
                        {
                            if (vars[varsIndex] == -1)
                            {
                                sw.Write($", {(varsIndex + 1) * 10}: -1");
                            }
                            else
                            {
                                sw.Write($", {(varsIndex + 1) * 10}: " + vars[varsIndex]);
                            }
                        }
                        sw.Write(";");
                    }

                    sw.WriteLine();
                }
            }
        }
    }
}
