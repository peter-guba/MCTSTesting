using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Linq;
using System.Diagnostics;

namespace data_cruncher
{
    internal class PrimaryDataCruncher
    {
        /// <summary>
        /// The maximum hull of a battleship unit.
        /// </summary>
        private const double battleship_hull = 21;

        /// <summary>
        /// The maximum hull of a destroyer unit.
        /// </summary>
        private const double destroyer_hull = 10.5;

        /// <summary>
        /// The maximum hp of a light unit.
        /// </summary>
        private const double light_hp = 4;

        /// <summary>
        /// The maximum hp of a heavy unit.
        /// </summary>
        private const double heavy_hp = 4;

        /// <summary>
        /// The maximum value of a chess board.
        /// </summary>
        private const double chessMaxBoardValue = 8 * 100 + 2 * 300 + 2 * 320 + 2 * 500 + 900 + 1200;

        /// <summary>
        /// A variable that stores all the loaded data. It is a triply-nested dictionary. The keys of the first layer are
        /// two-tuples consisting of a string which represents the combat setting and an int which represents the playout setting.
        /// For every combat-playout setting tuple therefore, we have a double-nested dictionary which represents all the data gathered
        /// about that touple. One can imagine this data as an excel spreadsheet (hence the name) where one needs a row and column
        /// to identify a single cell. The first layer represents the rows while the second represents the columns. The keys are
        /// the names of MCTS variants in both cases. The value stored in the last layer is a 6-touple consisting of two ints that
        /// correspond to the number of wins and symwins, two lists of doubles which correspond to the accumulated hull and damage
        /// and two more ints which correspond to the number of draws and unfinished games.
        /// </summary>
        private static Dictionary<(string, int), Dictionary<string, Dictionary<string, (int, int, List<double>, List<double>, int, int)>>> sheets =
            new Dictionary<(string, int), Dictionary<string, Dictionary<string, (int, int, List<double>, List<double>, int, int)>>>();

        public static void CrunchPrimaryData(string[] args)
        {
            DirectoryInfo di = new DirectoryInfo(Cruncher.sourcePath + "results");

            List<string> files = new List<FileInfo>(di.GetFiles("*.*", SearchOption.AllDirectories)).Select(x => x.FullName).ToList();
            List<string> fileNames = new List<FileInfo>(di.GetFiles("*.*", SearchOption.AllDirectories)).Select(x => x.Name).ToList();

            // A regex used for fetching the names of MCTS variants from the file name.
            Regex r = new Regex("(.*)_([0-9]*)_vs_(.*)_([0-9]*).csv");

            Console.WriteLine("Beginning primary data crunching.");

            // Go through all the files and load their data. The files are expected to be in csv format.
            for (int fileIndex = 0; fileIndex < files.Count; ++fileIndex)
            {
                Console.WriteLine($"    Loading file {fileNames[fileIndex]}.");

                using (StreamReader sr = new StreamReader(files[fileIndex]))
                {
                    string prevBattle = "";

                    List<string> columns = new List<string>(sr.ReadLine().Split("; "));

                    int wins1 = 0;
                    int wins2 = 0;
                    int symwins1 = 0;
                    int symwins2 = 0;
                    List<double> hp1 = new List<double>();
                    List<double> hp2 = new List<double>();
                    List<double> damage1 = new List<double>();
                    List<double> damage2 = new List<double>();
                    int draws = 0;
                    int unfinished = 0;

                    string battle = "";
                    string p1 = "";
                    string p2 = "";
                    int playouts = 0;

                    int prevWin = 0;
                    bool iterTwo = false;
                    int prevWinner = 0;

                    string[] row;
                    while (sr.Peek() != -1)
                    {
                        row = sr.ReadLine().Split(';');

                        //p1 = ConvertName(r.Match(fileNames[fileIndex]).Groups[1].Value);
                        //p2 = ConvertName(r.Match(fileNames[fileIndex]).Groups[3].Value);

                        p1 = r.Match(fileNames[fileIndex]).Groups[1].Value;
                        p2 = r.Match(fileNames[fileIndex]).Groups[3].Value;

                        battle = row[columns.IndexOf("battleName")];
                        double totalHP = GetTotalHP(Cruncher.type, battle);
                        playouts = int.Parse(fileNames[fileIndex].Substring(fileNames[fileIndex].LastIndexOf('_') + 1, fileNames[fileIndex].LastIndexOf('.') - fileNames[fileIndex].LastIndexOf('_') - 1));

                        if (prevBattle != battle && prevBattle != "")
                        {
                            AddToSheets(p1, p2, prevBattle, playouts, wins1, symwins1, hp1, damage1, draws, unfinished);
                            AddToSheets(p2, p1, prevBattle, playouts, wins2, symwins2, hp2, damage2, draws, unfinished);

                            wins1 = 0;
                            wins2 = 0;
                            symwins1 = 0;
                            symwins2 = 0;
                            hp1 = new List<double>();
                            hp2 = new List<double>();
                            damage1 = new List<double>();
                            damage2 = new List<double>();
                            draws = 0;
                            unfinished = 0;
                        }

                        wins1 += int.Parse(row[columns.IndexOf("p1Win")]);
                        wins2 += int.Parse(row[columns.IndexOf("p2Win")]);
                        hp1.Add(double.Parse(row[columns.IndexOf("p1hull")], CultureInfo.InvariantCulture));
                        hp2.Add(double.Parse(row[columns.IndexOf("p2hull")], CultureInfo.InvariantCulture));

                        if (Cruncher.type == Cruncher.GameType.Chess)
                        {
                            if (
                                int.Parse(row[columns.IndexOf("p1Win")]) == 0 &&
                                int.Parse(row[columns.IndexOf("p2Win")]) == 0
                                )
                            {
                                ++unfinished;
                            }
                        }
                        else
                        {
                            if (double.Parse(row[columns.IndexOf("p1hull")]) != 0.0 &&
                                double.Parse(row[columns.IndexOf("p2hull")]) != 0.0)
                            {
                                ++unfinished;
                            }

                            if (double.Parse(row[columns.IndexOf("p1hull")]) == double.Parse(row[columns.IndexOf("p2hull")]))
                            {
                                ++draws;
                            }
                        }

                        if (!iterTwo)
                        {
                            prevWinner = int.Parse(row[columns.IndexOf("p1Win")]) * 1 + int.Parse(row[columns.IndexOf("p2Win")]) * 2;
                        }
                        else
                        {
                            int symWinner = 0;
                            int winner = int.Parse(row[columns.IndexOf("p1Win")]) * 1 + int.Parse(row[columns.IndexOf("p2Win")]) * 2;
                            if (prevWinner != 0 && prevWinner == winner)
                            {
                                symWinner = winner;
                            }
                            else if (prevWinner != 0 && winner == 0)
                            {
                                symWinner = prevWinner;
                            }
                            else if (prevWinner == 0 && winner != 0)
                            {
                                symWinner = winner;
                            }
                            else if (prevWinner != 0 && winner != 0)
                            {
                                if (prevWinner == 1)
                                {
                                    symWinner = hp1[hp1.Count - 2] > hp2[hp2.Count - 1] ? 1 : hp1[hp1.Count - 2] < hp2[hp2.Count - 1] ? 2 : 0;
                                }
                                else
                                {
                                    symWinner = hp1[hp1.Count - 1] > hp2[hp2.Count - 2] ? 1 : hp1[hp1.Count - 1] < hp2[hp2.Count - 2] ? 2 : 0;
                                }
                            }
                            else
                            {
                                symWinner = hp1[hp1.Count - 1] + hp1[hp1.Count - 2] > hp2[hp2.Count - 1] + hp2[hp2.Count - 2] ? 1 :
                                    hp1[hp1.Count - 1] + hp1[hp1.Count - 2] < hp2[hp2.Count - 1] + hp2[hp2.Count - 2] ? 2 : 0;
                            }

                            if (symWinner == 1)
                            {
                                ++symwins1;
                            }
                            else if (symWinner == 2)
                            {
                                ++symwins2;
                            }
                        }

                        damage1.Add(totalHP - hp2.Last());
                        damage2.Add(totalHP - hp1.Last());

                        if (!sheets.ContainsKey((battle, playouts)))
                        {
                            sheets.Add((battle, playouts), new Dictionary<string, Dictionary<string, (int, int, List<double>, List<double>, int, int)>>());
                        }

                        if (!sheets[(battle, playouts)].ContainsKey(p1))
                        {
                            sheets[(battle, playouts)].Add(p1, new Dictionary<string, (int, int, List<double>, List<double>, int, int)>());
                        }

                        if (!sheets[(battle, playouts)].ContainsKey(p2))
                        {
                            sheets[(battle, playouts)].Add(p2, new Dictionary<string, (int, int, List<double>, List<double>, int, int)>());
                        }

                        prevBattle = battle;
                        iterTwo = !iterTwo;
                    }

                    AddToSheets(p1, p2, battle, playouts, wins1, symwins1, hp1, damage1, draws, unfinished);
                    AddToSheets(p2, p1, battle, playouts, wins2, symwins2, hp2, damage2, draws, unfinished);
                }
            }

            Console.WriteLine();

            // Add dummy data for confrontations between same variants.
            foreach (var sheet in sheets.Values)
            {
                foreach (string key in sheet.Keys)
                {
                    sheet[key].Add(key, (0, 0, new List<double>(), new List<double>(), 0, 0));
                }
            }

            string[] metricNames = new string[] { "wins", "symwins", "hulls", "damages", "draws", "unfinished" };
            int[] playoutSettings = new int[] { 1000, 5000, 10000 };
            string[] battleSettings = new string[] { "4v4_2D-2B", "8v8_4D-4B", "16v16_8D-8B" };

            if (!Directory.Exists(Cruncher.outputPath + "primary data"))
            {
                Directory.CreateDirectory(Cruncher.outputPath + "primary data");
                Console.WriteLine("Primary data folder created.");
                Console.WriteLine();
            }

            Console.WriteLine("Making tables.");

            /*/foreach (int ps in playoutSettings)
            {
                foreach (string bs in battleSettings)
                {
                    Console.WriteLine($" Making table {Cruncher.tableBaseName}" + ps.ToString() + "_" + bs + ".");

                    MakeCSVTable(
                        Cruncher.outputPath + $"/primary data/{Cruncher.tableBaseName}" + ps.ToString() + "_" + bs + ".csv",
                        false,
                        null,
                        new List<string>() { bs },
                        new List<int>() { ps }
                        );
                }
            }

            foreach (int ps in playoutSettings)
            {
                Console.WriteLine($" Making table {Cruncher.tableBaseName}" + ps.ToString() + ".");

                MakeCSVTable(
                    Cruncher.outputPath + $"/primary data/{Cruncher.tableBaseName}" + ps.ToString() + ".csv",
                    false,
                    null,
                    new List<string>() { "4v4_2D-2B", "8v8_4D-4B", "16v16_8D-8B" },
                    new List<int>() { ps }
                    );
            }

            foreach (string bs in battleSettings)
            {
                Console.WriteLine($" Making table {Cruncher.tableBaseName}" + bs + ".");

                MakeCSVTable(
                    Cruncher.outputPath + $"/primary data/{Cruncher.tableBaseName}" + bs + ".csv",
                    false,
                    null,
                    new List<string>() { bs },
                    new List<int>() { 1000, 5000, 10000 }
                    );
            }

            Console.WriteLine($" Making table {Cruncher.tableBaseName}all.");

            MakeCSVTable(
                Cruncher.outputPath + $"/primary data/{Cruncher.tableBaseName}all.csv",
                false,
                null,
                new List<string>() { "4v4_2D-2B", "8v8_4D-4B", "16v16_8D-8B" },
                new List<int>() { 1000, 5000, 10000 }
                );/**/

            /*/for (int mn = 0; mn < 4; ++mn)
            {
                for (int ps = 0; ps < playoutSettings.Length; ++ps)
                {
                    using (StreamWriter sw = new StreamWriter(Cruncher.outputPath + '/' + metricNames[mn] + ' ' + playoutSettings[ps] + ".txt"))
                    {
                        var results = GetSortedMetricData(mn, new List<string>() { "4v4_2D-2B", "8v8_4D-4B", "16v16_8D-8B" }, new List<int>() { playoutSettings[ps] });

                        foreach (var res in results)
                        {
                            sw.WriteLine($"{res.Key}: {res.Value}");
                        }
                    }
                }

                for (int bs = 0; bs < battleSettings.Length; ++bs)
                {
                    using (StreamWriter sw = new StreamWriter(Cruncher.outputPath + '/' + metricNames[mn] + ' ' + battleSettings[bs] + ".txt"))
                    {
                        var results = GetSortedMetricData(mn, new List<string>() { battleSettings[bs] }, new List<int>() { 1000, 5000, 10000 });

                        foreach (var res in results)
                        {
                            sw.WriteLine($"{res.Key}: {res.Value}");
                        }
                    }
                }

                using (StreamWriter sw = new StreamWriter(Cruncher.outputPath + '/' + metricNames[mn] + " all.txt"))
                {
                    var results = GetSortedMetricData(mn, new List<string>() { "4v4_2D-2B", "8v8_4D-4B", "16v16_8D-8B" }, new List<int>() { 1000, 5000, 10000 });

                    foreach (var res in results)
                    {
                        sw.WriteLine($"{res.Key}: {res.Value}");
                    }
                }
            }

            using (StreamWriter sw = new StreamWriter(Cruncher.outputPath + "/choices.txt"))
            {
                var wins = GetSortedMetricData(0, new List<string>() { "4v4_2D-2B", "8v8_4D-4B", "16v16_8D-8B" }, new List<int>() { 1000, 5000, 10000 });
                var symwins = GetMetricData(1, new List<string>() { "4v4_2D-2B", "8v8_4D-4B", "16v16_8D-8B" }, new List<int>() { 1000, 5000, 10000 });
                var hulls = GetMetricData(2, new List<string>() { "4v4_2D-2B", "8v8_4D-4B", "16v16_8D-8B" }, new List<int>() { 1000, 5000, 10000 });
                var damages = GetMetricData(3, new List<string>() { "4v4_2D-2B", "8v8_4D-4B", "16v16_8D-8B" }, new List<int>() { 1000, 5000, 10000 });

                for (int i = 0; i < wins.Count; ++i)
                {
                    sw.WriteLine($"{wins[i].Key}: {wins[i].Value}, {symwins[wins[i].Key]}, {hulls[wins[i].Key]}, {damages[wins[i].Key]}");
                }
            }/**/

            using (StreamWriter sw = new StreamWriter(Cruncher.outputPath + "/win tallys.txt"))
            {
                Dictionary<(string, string), (int, int, int)> data = new Dictionary<(string, string), (int, int, int)>();

                foreach ((string, int) setting in sheets.Keys)
                {
                    foreach (string variant in sheets[setting].Keys)
                    {
                        if (variant.StartsWith("Mix"))
                        {
                            foreach (string opponent in sheets[setting][variant].Keys)
                            {
                                if (variant != opponent)
                                {
                                    if (!data.ContainsKey((variant, opponent)))
                                    {
                                        data.Add(
                                            (variant, opponent),
                                            (
                                                sheets[setting][variant][opponent].Item1,
                                                sheets[setting][opponent][variant].Item1,
                                                336 - sheets[setting][variant][opponent].Item1 - sheets[setting][opponent][variant].Item1
                                            )
                                            );
                                    }
                                    else
                                    {
                                        data[(variant, opponent)] = (
                                                data[(variant, opponent)].Item1 + sheets[setting][variant][opponent].Item1,
                                                data[(variant, opponent)].Item2 + sheets[setting][opponent][variant].Item1,
                                                data[(variant, opponent)].Item3 + 336 - sheets[setting][variant][opponent].Item1 - sheets[setting][opponent][variant].Item1
                                            );
                                    }
                                }
                            }
                        }
                    }
                }

                foreach (var datum in data)
                {
                    sw.WriteLine(
                        datum.Key.Item1 + " vs " +
                        datum.Key.Item2 + ": " +
                        datum.Value.Item1 + "w, " +
                        datum.Value.Item2 + "l, " +
                        datum.Value.Item3 + "d"
                        );
                }
            }
        }

        /// <summary>
        /// Adds the given data to the sheets variable, creating a new entry if necessary.
        /// </summary>
        private static void AddToSheets(string p1, string p2, string battle, int playouts,
            int wins, int symwins, List<double> hp, List<double> damage, int draws, int unfinished)
        {
            if (sheets[(battle, playouts)][p1].ContainsKey(p2))
            {
                (int, int, List<double>, List<double>, int, int) updatedResults = (
                    wins + sheets[(battle, playouts)][p1][p2].Item1,
                    symwins + sheets[(battle, playouts)][p1][p2].Item2,
                    hp,
                    damage,
                    draws + sheets[(battle, playouts)][p1][p2].Item5,
                    unfinished + sheets[(battle, playouts)][p1][p2].Item6
                    );

                updatedResults.Item3.AddRange(sheets[(battle, playouts)][p1][p2].Item3);
                updatedResults.Item4.AddRange(sheets[(battle, playouts)][p1][p2].Item4);

                sheets[(battle, playouts)][p1][p2] = updatedResults;
            }
            else
            {
                sheets[(battle, playouts)][p1].Add(p2, (wins, symwins, hp, damage, draws, unfinished));
            }
        }

        /// <summary>
        /// Converts the name of a variant to a desirable format.
        /// </summary>
        /// <param name="original"></param>
        /// <returns></returns>
        private static string ConvertName(string original)
        {
            // WP MCTS used to be called AP MCTS which is why the "AP" prefix is
            // present here.
            if (original.StartsWith("AP") || original.StartsWith("WP"))
            {
                return "WP";
            }
            else if (original.StartsWith("Basic"))
            {
                return "UCT";
            }
            else if (original.StartsWith("FAP"))
            {
                return "FAP";
            }
            else if (original.StartsWith("MCTS_HP"))
            {
                return "HP";
            }
            else if (original.StartsWith("Q_Bonus"))
            {
                return "QB";
            }
            else if (original.StartsWith("R_Bonus"))
            {
                return "RB";
            }
            else if (original.StartsWith("R_Q_Bonus"))
            {
                return "RQB";
            }
            else if (original.StartsWith("Sigmoid"))
            {
                return "Sig";
            }
            else if (original.StartsWith("Simple"))
            {
                return "SR";
            }
            else if (original.StartsWith("UCB"))
            {
                return "U-T";
            }
            else if (original.StartsWith("VOI"))
            {
                return "VOI";
            }

            throw new Exception($"unknown name: {original}");
        }

        private static double GetTotalHP(Cruncher.GameType type, string battle)
        {
            switch (type)
            {
                case Cruncher.GameType.CotG:
                    return (int.Parse(battle.Substring(battle.IndexOf('_') + 1, battle.IndexOf('D') - battle.IndexOf('_') - 1)) * destroyer_hull +
                            int.Parse(battle.Substring(battle.IndexOf('-') + 1, battle.IndexOf('B') - battle.IndexOf('-') - 1)) * battleship_hull);
                case Cruncher.GameType.MicroRTS:
                    return (int.Parse(battle.Substring(battle.IndexOf('_') + 1, battle.IndexOf('D') - battle.IndexOf('_') - 1)) * heavy_hp +
                            int.Parse(battle.Substring(battle.IndexOf('-') + 1, battle.IndexOf('B') - battle.IndexOf('-') - 1)) * light_hp);
                case Cruncher.GameType.Chess:
                    return chessMaxBoardValue;
                default: throw new Exception("wat");
            }
        }

        private static List<KeyValuePair<string, double>> GetSortedMetricData(int metricIndex, List<string> battles = null, List<int> playouts = null)
        {
            var results = GetMetricData(metricIndex, battles, playouts);
            return (from entry in results orderby entry.Value descending select entry).ToList();
        }

        private static Dictionary<string, double> GetMetricData(int metricIndex, List<string> battles = null, List<int> playouts = null)
        {            
            var keys = sheets.Keys;
            List<Dictionary<string, Dictionary<string, (int, int, List<double>, List<double>, int, int)>>> toProcess = new List<Dictionary<string, Dictionary<string, (int, int, List<double>, List<double>, int, int)>>>();

            foreach (var key in keys)
            {
                if ((battles == null || battles.Contains(key.Item1)) && (playouts == null || playouts.Contains(key.Item2)))
                {
                    toProcess.Add(sheets[key]);
                }
            }

            Dictionary<string, double> results = new Dictionary<string, double>();

            List<string> rows = new List<string>(toProcess[0].Keys);
            rows.Sort();

            foreach (var row in rows)
            {
                results.Add(row, 0.0);

                List<string> columns = new List<string>(toProcess[0][row].Keys);
                columns.Sort();

                foreach (var column in columns)
                {
                    int checkCount = -1;

                    double metric = 0.0;

                    foreach (var sheet in toProcess)
                    {
                        if (row != column && sheet[row].ContainsKey(column))
                        {
                            Debug.Assert(checkCount == -1 || checkCount == sheet[row][column].Item3.Count);

                            switch (metricIndex)
                            {
                                case 0: metric += sheet[row][column].Item1; break;
                                case 1: metric += sheet[row][column].Item2; break;
                                case 2: metric += sheet[row][column].Item3.Sum(); break;
                                case 3: metric += sheet[row][column].Item4.Sum(); break;
                                case 4: metric += sheet[row][column].Item5; break;
                                case 5: metric += sheet[row][column].Item6; break;
                                default: throw new Exception("Invalid metric index");
                            }

                            checkCount = sheet[row][column].Item3.Count;
                        }
                    }

                    results[row] += metric;
                }
            }

            return results;
        }

        /// <summary>
        /// Fetches data from the sheets variable.
        /// </summary>
        /// <param name="average"> Determines whether the data should be averaged or just summed. </param>
        /// <param name="battles"> A list of battle settings that are supposed to be taken into account. If this parameter is null,
        /// it means that the program is supposed to use all settings. </param>
        /// <param name="playouts"> A list of playout settings that are supposed to be taken into account. If this parameter is null,
        /// it means that the program is supposed to use all settings. </param>
        /// <returns> A new "sheet" with the corresponding data. </returns>
        private static Dictionary<string, Dictionary<string, (double, double, double, double, double, double, double, double, double, double, double, double)>> MakeSheet(bool average, List<string> battles = null, List<int> playouts = null)
        {
            var keys = sheets.Keys;
            List<Dictionary<string, Dictionary<string, (int, int, List<double>, List<double>, int, int)>>> toProcess = new List<Dictionary<string, Dictionary<string, (int, int, List<double>, List<double>, int, int)>>>();

            foreach (var key in keys)
            {
                if ((battles == null || battles.Contains(key.Item1)) && (playouts == null || playouts.Contains(key.Item2)))
                {
                    toProcess.Add(sheets[key]);
                }
            }

            Dictionary<string, Dictionary<string, (double, double, double, double, double, double, double, double, double, double, double, double)>> result = new Dictionary<string, Dictionary<string, (double, double, double, double, double, double, double, double, double, double, double, double)>>();

            List<string> rows = new List<string>(toProcess[0].Keys);
            rows.Sort();

            List<string> columns = new List<string>(toProcess[0][rows[0]].Keys);
            columns.Sort();

            foreach (var row in rows)
            {
                result.Add(row, new Dictionary<string, (double, double, double, double, double, double, double, double, double, double, double, double)>());

                double totalWins = 0.0;
                double totalSymWins = 0.0;
                double totalHp = 0.0;
                double totalDamage = 0.0;
                double totalDraws = 0.0;
                double totalUnfinished = 0.0;

                double totalWinsMean, totalSymWinsMean, totalHpMean, totalDamageMean, totalDrawsMean, totalUnfinishedMean;

                double totalWinsStdDev = 0.0;
                double totalSymWinsStdDev = 0.0;
                double totalHpStdDev = 0.0;
                double totalDamageStdDev = 0.0;
                double totalDrawsStdDev = 0.0;
                double totalUnfinishedStdDev = 0.0;

                double totalWinsConfBound, totalSymWinsConfBound, totalHpConfBound, totalDamageConfBound, totalDrawsConfBound, totalUnfinishedConfBound;

                int totalSampleCount = 0;

                foreach (var column in columns)
                {
                    int checkCount = -1;

                    double wins = 0.0;
                    double symWins = 0.0;
                    double hp = 0.0;
                    double damage = 0.0;
                    double draws = 0.0;
                    double unfinished = 0.0;

                    double winsMean, symWinsMean, hpMean, damageMean, drawsMean, unfinishedMean;

                    double winsStdDev = 0.0;
                    double symWinsStdDev = 0.0;
                    double hpStdDev = 0.0;
                    double damageStdDev = 0.0;
                    double drawsStdDev = 0.0;
                    double unfinishedStdDev = 0.0;

                    double winsConfBound, symWinsConfBound, hpConfBound, damageConfBound, drawsConfBound, unfinishedConfBound;

                    foreach (var sheet in toProcess)
                    {
                        Debug.Assert(checkCount == -1 || checkCount == sheet[row][column].Item3.Count);

                        wins += sheet[row][column].Item1;
                        symWins += sheet[row][column].Item2;
                        hp += sheet[row][column].Item3.Sum();
                        damage += sheet[row][column].Item4.Sum();
                        draws += sheet[row][column].Item5;
                        unfinished += sheet[row][column].Item6;

                        checkCount = sheet[row][column].Item3.Count;
                    }

                    totalWins += wins;
                    totalSymWins += symWins;
                    totalHp += hp;
                    totalDamage += damage;
                    totalDraws += draws;
                    totalUnfinished += unfinished;
                    totalSampleCount += checkCount * toProcess.Count;

                    // For the cases when the row and column names are the same, since
                    // there is no data for a variant fighting against itself.
                    if (checkCount == 0)
                    {
                        checkCount = 1;
                    }

                    int count = toProcess.Count;

                    winsMean = wins / (toProcess.Count * checkCount);
                    symWinsMean = symWins / (toProcess.Count * checkCount / 2);
                    hpMean = hp / (toProcess.Count * checkCount);
                    damageMean = damage / (toProcess.Count * checkCount);
                    drawsMean = draws / (toProcess.Count * checkCount);
                    unfinishedMean = unfinished / (toProcess.Count * checkCount);

                    foreach (var sheet in toProcess)
                    {
                        winsStdDev += sheet[row][column].Item1 * Math.Pow(1 - winsMean, 2);
                        winsStdDev += (count * checkCount - sheet[row][column].Item1) * Math.Pow(0 - winsMean, 2);
                        symWinsStdDev += sheet[row][column].Item2 * Math.Pow(1 - symWinsMean, 2);
                        symWinsStdDev += (count * checkCount / 2 - sheet[row][column].Item2) * Math.Pow(0 - symWinsMean, 2);

                        foreach (var points in sheet[row][column].Item3)
                        {
                            hpStdDev += Math.Pow(points - hpMean, 2);
                        }

                        foreach (var points in sheet[row][column].Item4)
                        {
                            damageStdDev += Math.Pow(points - damageMean, 2);
                        }

                        drawsStdDev += sheet[row][column].Item5 * Math.Pow(1 - drawsMean, 2);
                        drawsStdDev += (count * checkCount - sheet[row][column].Item5) * Math.Pow(0 - drawsMean, 2);
                        unfinishedStdDev += sheet[row][column].Item6 * Math.Pow(1 - unfinishedMean, 2);
                        unfinishedStdDev += (count * checkCount - sheet[row][column].Item6) * Math.Pow(0 - unfinishedMean, 2);

                        checkCount = sheet[row][column].Item3.Count;
                    }

                    winsStdDev = count * checkCount > 1 ? Math.Sqrt(winsStdDev / (count * checkCount - 1)) : 0.0;
                    symWinsStdDev = count * checkCount > 1 ? Math.Sqrt(symWinsStdDev / (count * checkCount / 2 - 1)) : 0.0;
                    hpStdDev = Math.Sqrt(hpStdDev / (count * checkCount - 1));
                    damageStdDev = Math.Sqrt(damageStdDev / (count * checkCount - 1));
                    drawsStdDev = count * checkCount > 1 ? Math.Sqrt(drawsStdDev / (count * checkCount - 1)) : 0.0;
                    unfinishedStdDev = count * checkCount > 1 ? Math.Sqrt(unfinishedStdDev / (count * checkCount - 1)) : 0.0;

                    winsConfBound = Cruncher.zValue * (winsStdDev / Math.Sqrt(count * checkCount));
                    symWinsConfBound = Cruncher.zValue * (symWinsStdDev / Math.Sqrt(count * checkCount / 2));
                    hpConfBound = Cruncher.zValue * (hpStdDev / Math.Sqrt(count * checkCount));
                    damageConfBound = Cruncher.zValue * (damageStdDev / Math.Sqrt(count * checkCount));
                    drawsConfBound = Cruncher.zValue * (drawsStdDev / Math.Sqrt(count * checkCount));
                    unfinishedConfBound = Cruncher.zValue * (unfinishedStdDev / Math.Sqrt(count * checkCount));

                    if (average)
                    {
                        result[row].Add(column, (winsMean, winsConfBound, symWinsMean, symWinsConfBound, hpMean, hpConfBound, damageMean, damageConfBound, drawsMean, drawsConfBound, unfinishedMean, unfinishedConfBound));
                    }
                    else
                    {
                        result[row].Add(column, (wins, winsConfBound, symWins, symWinsConfBound, hp, hpConfBound, damage, damageConfBound, draws, drawsConfBound, unfinished, unfinishedConfBound));
                    }
                }

                totalWinsMean = totalWins / (totalSampleCount);
                totalSymWinsMean = totalSymWins / (totalSampleCount / 2);
                totalHpMean = totalHp / (totalSampleCount);
                totalDamageMean = totalDamage / (totalSampleCount);
                totalDrawsMean = totalDraws / (totalSampleCount);
                totalUnfinishedMean = totalUnfinished / (totalSampleCount);

                foreach (var column in columns)
                {
                    foreach (var sheet in toProcess)
                    {
                        if (sheet[row][column].Item3.Count != 0)
                        {
                            totalWinsStdDev += sheet[row][column].Item1 * Math.Pow(1 - totalWinsMean, 2);
                            totalWinsStdDev += (totalSampleCount - sheet[row][column].Item1) * Math.Pow(0 - totalWinsMean, 2);
                            totalSymWinsStdDev += sheet[row][column].Item2 * Math.Pow(1 - totalSymWinsMean, 2);
                            totalSymWinsStdDev += (totalSampleCount / 2 - sheet[row][column].Item2) * Math.Pow(0 - totalSymWinsMean, 2);

                            foreach (var points in sheet[row][column].Item3)
                            {
                                totalHpStdDev += Math.Pow(points - totalHpMean, 2);
                            }

                            foreach (var points in sheet[row][column].Item4)
                            {
                                totalDamageStdDev += Math.Pow(points - totalDamageMean, 2);
                            }

                            totalDrawsStdDev += sheet[row][column].Item5 * Math.Pow(1 - totalDrawsMean, 2);
                            totalDrawsStdDev += (totalSampleCount - sheet[row][column].Item5) * Math.Pow(0 - totalDrawsMean, 2);
                            totalUnfinishedStdDev += sheet[row][column].Item6 * Math.Pow(1 - totalUnfinishedMean, 2);
                            totalUnfinishedStdDev += (totalSampleCount - sheet[row][column].Item5) * Math.Pow(0 - totalUnfinishedMean, 2);
                        }
                    }
                }

                totalWinsStdDev = totalSampleCount > 1 ? Math.Sqrt(totalWinsStdDev / (totalSampleCount - 1)) : 0.0;
                totalSymWinsStdDev = totalSampleCount > 1 ? Math.Sqrt(totalSymWinsStdDev / (totalSampleCount / 2 - 1)) : 0.0;
                totalHpStdDev = Math.Sqrt(totalHpStdDev / (totalSampleCount - 1));
                totalDamageStdDev = Math.Sqrt(totalDamageStdDev / (totalSampleCount - 1));
                totalDrawsStdDev = totalSampleCount > 1 ? Math.Sqrt(totalDrawsStdDev / (totalSampleCount - 1)) : 0.0;
                totalUnfinishedStdDev = totalSampleCount > 1 ? Math.Sqrt(totalUnfinishedStdDev / (totalSampleCount - 1)) : 0.0;

                totalWinsConfBound = Cruncher.zValue * (totalWinsStdDev / Math.Sqrt(totalSampleCount));
                totalSymWinsConfBound = Cruncher.zValue * (totalSymWinsStdDev / Math.Sqrt(totalSampleCount / 2));
                totalHpConfBound = Cruncher.zValue * (totalHpStdDev / Math.Sqrt(totalSampleCount));
                totalDamageConfBound = Cruncher.zValue * (totalDamageStdDev / Math.Sqrt(totalSampleCount));
                totalDrawsConfBound = Cruncher.zValue * (totalDrawsStdDev / Math.Sqrt(totalSampleCount));
                totalUnfinishedConfBound = Cruncher.zValue * (totalUnfinishedStdDev / Math.Sqrt(totalSampleCount));

                if (average) {
                    result[row].Add("total", (totalWinsMean, totalWinsConfBound, totalSymWinsMean, totalSymWinsConfBound, totalHpMean, totalHpConfBound, totalDamageMean, totalDamageConfBound, totalDrawsMean, totalDrawsConfBound, totalUnfinishedMean, totalUnfinishedConfBound));
                }
                else
                {
                    result[row].Add("total", (totalWins, totalWinsConfBound, totalSymWins, totalSymWinsConfBound, totalHp, totalHpConfBound, totalDamage, totalDamageConfBound, totalDraws, totalDrawsConfBound, totalUnfinished, totalUnfinishedConfBound));
                }
            }

            return result;
        }

        /// <summary>
        /// Fetches data from sheets and creates a csv table.
        /// </summary>
        /// <param name="path"> Path to the output file. </param>
        /// <param name="average"> Determines whether the data is supposed to be averaged or just summed. </param>
        /// <param name="indices"> The indices of the metrics that are supposed to be present in the table (0 <=> wins, 1 <=> symwins
        /// 2 <=> hull, 3 <=> damage). </param>
        /// <param name="battles"> A list of battle settings that are supposed to be taken into account. If this parameter is null,
        /// it means that the program is supposed to use all settings. </param>
        /// <param name="playouts"> A list of playout settings that are supposed to be taken into account. If this parameter is null,
        /// it means that the program is supposed to use all settings. </param>
        private static void MakeCSVTable(string path, bool average, List<int> indices, List<string> battles = null, List<int> playouts = null)
        {
            var sheet = MakeSheet(average, battles, playouts);

            using (StreamWriter sw = new StreamWriter(path))
            {
                var rows = new List<string>(sheet.Keys);
                var columns = sheet[rows[0]].Keys;

                foreach (string variant in columns)
                {
                    sw.Write("; " + variant);
                }
                sw.WriteLine();

                foreach (string row in rows)
                {
                    sw.Write(row);
                    foreach (string column in columns)
                    {
                        if (row == column)
                        {
                            sw.Write("; X");
                        }
                        else
                        {
                            sw.Write("; " + GetStringFromData(sheet[row][column], 1, indices));
                        }
                    }
                    sw.WriteLine();
                }
            }
        }

        /// <summary>
        /// Fetches data from sheets and creates the body of a latex table (without the \begin{table} and \end{table} lines).
        /// </summary>
        /// <param name="average"> Determines whether the data is supposed to be averaged or just summed. </param>
        /// <param name="sw"> Stream writer that is to be used as output. </param>
        /// <param name="title"> The title of the table  </param>
        /// <param name="divFactor"> The number by which data are supposed to be divided. </param>
        /// <param name="printAsInt"> Determines whether the data should be printed as an integer (without decimal places) or not. </param>
        /// <param name="indices"> The indices of the metrics that are supposed to be present in the table (0 <=> wins, 1 <=> symwins
        /// 2 <=> hull, 3 <=> damage). </param>
        /// <param name="battles"> A list of battle settings that are supposed to be taken into account. If this parameter is null,
        /// it means that the program is supposed to use all settings. </param>
        /// <param name="playouts"> A list of playout settings that are supposed to be taken into account. If this parameter is null,
        /// it means that the program is supposed to use all settings. </param>
        private static void MakeLatexTable(
            bool average,
            StreamWriter sw,
            string title,
            int divFactor,
            bool printAsInt,
            List<int> indices,
            List<string> battles = null,
            List<int> playouts = null)
        {
            var sheet = MakeSheet(average, battles, playouts);
            List<string> rows = new List<string>(sheet.Keys);
            List<string> columns = new List<string>(sheet[rows[0]].Keys);

            sw.WriteLine("\\begin{tabular}{ |l||c|c|c|c|c|c|c|c|c|c| }");
            sw.WriteLine("\\hline");
            sw.WriteLine($"\\multicolumn{{11}}{{|c|}}{{{title}}} \\\\");
            sw.WriteLine("\\hline");
            sw.WriteLine("&" + string.Join('&', columns) + "\\\\");
            sw.WriteLine("\\hline");

            foreach (var row in rows)
            {
                sw.Write(row);
                foreach (var column in columns)
                {
                    if (row == column)
                    {
                        sw.Write("&X");
                    }
                    else
                    {
                        if (printAsInt)
                        {
                            sw.Write("&");

                            for (int i = 0; i < indices.Count; ++i)
                            {
                                if (indices[i] > 3)
                                {
                                    throw new Exception("bad index");
                                }
                                else
                                {
                                    if (indices[i] == 0)
                                    {
                                        sw.Write(((int)(sheet[row][column].Item1)).ToString());
                                    }
                                    else if (indices[i] == 1)
                                    {
                                        sw.Write(((int)(sheet[row][column].Item2)).ToString());
                                    }
                                    else if (indices[i] == 2)
                                    {
                                        sw.Write(((int)(sheet[row][column].Item3)).ToString());
                                    }
                                    else if (indices[i] == 3)
                                    {
                                        sw.Write(((int)(sheet[row][column].Item4)).ToString());
                                    }

                                    if (i != indices.Count - 1)
                                    {
                                        sw.Write(", ");
                                    }
                                }
                            }
                        }
                        else
                        {
                            sw.Write("&" + GetStringFromData(sheet[row][column], divFactor, indices, 1));
                        }
                    }
                }
                sw.WriteLine("\\\\");
            }

            sw.WriteLine("\\hline");
            sw.WriteLine("\\end{tabular}");
        }

        /// <summary>
        /// Creates all the necessary Latex tables.
        /// </summary>
        private static void MakeAllLatexTables()
        {
            List<int> pSettings = new List<int>() { 100, 500, 1000 };
            List<string> cSettings1 = new List<string>() { "4v4_2D-2B", "8v8_4D-4B", "16v16_8D-8B" };
            List<string> cSettings2 = new List<string>() { "32v32_16D-16B", "48v48_24D-24B", "64v64_32D-32B" };
            Dictionary<string, string> cSetTextVersion = new Dictionary<string, string>()
            {
                { "4v4_2D-2B", "4 vs 4" },
                { "8v8_4D-4B", "8 vs 8" },
                { "16v16_8D-8B", "16 vs 16" },
                { "32v32_16D-16B", "32 vs 32" },
                { "48v48_24D-24B", "48 vs 48" },
                { "64v64_32D-32B", "64 vs 64" }
            };
            List<int> divFactorsP = new List<int>() { 1, 1, 1, 1 };
            List<int> divFactorsC = new List<int>() { 1, 1, 1, 1 };
            List<string> metricNames = new List<string>() { "wins", "symwins", "hull", "damage" };
            List<string> metricTexts = new List<string>() { "number of wins", "number of symwins", "amount of remaining hull", "amount of damage dealt to the enemy" };
            List<bool> printAsInt = new List<bool>() { true, true, false, false };

            using (StreamWriter sw = new StreamWriter("C:\\Users\\Peter Guba\\Desktop\\latex.txt"))
            {
                for (int index = 0; index < 4; ++index)
                {
                    sw.WriteLine("\\begin{table}");
                    sw.WriteLine("\\centering");
                    foreach (int numOfPlayouts in pSettings)
                    {
                        sw.WriteLine("\\begin{subtable}{0.9\\textwidth}");
                        sw.WriteLine("\\resizebox{\\textwidth}{!}{");
                        MakeLatexTable(false, sw, $"{metricNames[index]} ({numOfPlayouts} playouts)", divFactorsP[index], printAsInt[index], new List<int>() { index }, null, new List<int>() { numOfPlayouts });
                        sw.WriteLine("}");
                        sw.WriteLine("\\end{subtable}");
                        sw.WriteLine();
                    }
                    sw.WriteLine($"\\caption{{The {metricTexts[index]} that every variant scored against every other variant on every playout setting. Unlike the graphs section \\ref{{experiments}}, this data is summed, not averaged.}}");
                    sw.WriteLine("\\end{table}");
                    sw.WriteLine();
                }

                for (int index = 0; index < 4; ++index)
                {
                    sw.WriteLine("\\begin{table}");
                    sw.WriteLine("\\centering");
                    foreach (string combatSetting in cSettings1)
                    {
                        sw.WriteLine("\\begin{subtable}{0.9\\textwidth}");
                        sw.WriteLine("\\resizebox{\\textwidth}{!}{");
                        MakeLatexTable(false, sw, $"{metricNames[index]} ({cSetTextVersion[combatSetting]})", divFactorsC[index], printAsInt[index], new List<int>() { index }, new List<string>() { combatSetting }, null);
                        sw.WriteLine("}");
                        sw.WriteLine("\\end{subtable}");
                        sw.WriteLine();
                    }
                    sw.WriteLine($"\\caption{{The {metricTexts[index]} that every variant scored against every other variant in the first three combat settings. Unlike the graphs section \\ref{{experiments}}, this data is summed, not averaged.}}");
                    sw.WriteLine("\\end{table}");
                    sw.WriteLine();

                    sw.WriteLine("\\begin{table}");
                    sw.WriteLine("\\centering");
                    foreach (string combatSetting in cSettings2)
                    {
                        sw.WriteLine("\\begin{subtable}{0.9\\textwidth}");
                        sw.WriteLine("\\resizebox{\\textwidth}{!}{");
                        MakeLatexTable(false, sw, $"{metricNames[index]} ({cSetTextVersion[combatSetting]})", divFactorsC[index], printAsInt[index], new List<int>() { index }, new List<string>() { combatSetting }, null);
                        sw.WriteLine("}");
                        sw.WriteLine("\\end{subtable}");
                        sw.WriteLine();
                    }
                    sw.WriteLine($"\\caption{{The {metricTexts[index]} that every variant scored against every other variant in the second three combat settings. Unlike the graphs section \\ref{{experiments}}, this data is summed, not averaged.}}");
                    sw.WriteLine("\\end{table}");
                    sw.WriteLine();
                }
            }
        }

        /// <summary>
        /// Converts the given data to an appropriate string representation.
        /// </summary>
        /// <param name="data"> The individual items represent the average number of wins and symwins, the accumulated hull
        /// and the accumulated damage dealt to the enemy in that order. </param>
        /// <param name="divFactor"> A number by which the returned data are supposed to be divided. </param>
        /// <param name="indices"> Indices of the this function is supposed to put into a string. </param>
        /// <param name="decPlaces"> The number of decimal places to which the returned data should be rounded. </param>
        private static string GetStringFromData((double, double, double, double, double, double, double, double, double, double, double, double) data, int divFactor, List<int> indices = null, int decPlaces = 15)
        {
            string decPlaceString = "0.";

            for (int i = 0; i < decPlaces; ++i)
            {
                decPlaceString += "0";
            }

            if (indices == null)
            {
                return $"{Math.Round(data.Item1 / divFactor, decPlaces).ToString(decPlaceString, CultureInfo.InvariantCulture)}w, " +
                       $"{Math.Round(data.Item2 / divFactor, decPlaces).ToString(decPlaceString, CultureInfo.InvariantCulture)}wcb, " +
                       $"{Math.Round(data.Item3 / divFactor, decPlaces).ToString(decPlaceString, CultureInfo.InvariantCulture)}sw, " +
                       $"{Math.Round(data.Item4 / divFactor, decPlaces).ToString(decPlaceString, CultureInfo.InvariantCulture)}swcb, " +
                       $"{Math.Round(data.Item5 / divFactor, decPlaces).ToString(decPlaceString, CultureInfo.InvariantCulture)}h, " +
                       $"{Math.Round(data.Item6 / divFactor, decPlaces).ToString(decPlaceString, CultureInfo.InvariantCulture)}hcb, " +
                       $"{Math.Round(data.Item7 / divFactor, decPlaces).ToString(decPlaceString, CultureInfo.InvariantCulture)}d, " +
                       $"{Math.Round(data.Item8 / divFactor, decPlaces).ToString(decPlaceString, CultureInfo.InvariantCulture)}dcb, " +
                       $"{Math.Round(data.Item9 / divFactor, decPlaces).ToString(decPlaceString, CultureInfo.InvariantCulture)}dr, " +
                       $"{Math.Round(data.Item10 / divFactor, decPlaces).ToString(decPlaceString, CultureInfo.InvariantCulture)}drcb, " +
                       $"{Math.Round(data.Item11 / divFactor, decPlaces).ToString(decPlaceString, CultureInfo.InvariantCulture)}u, " +
                       $"{Math.Round(data.Item12 / divFactor, decPlaces).ToString(decPlaceString, CultureInfo.InvariantCulture)}ucb";
            }
            else
            {
                string result = "";

                for (int i = 0; i < indices.Count; ++i)
                {
                    if (indices[i] > 3)
                    {
                        throw new Exception("bad index");
                    }
                    else
                    {
                        if (indices[i] == 0)
                        {
                            result += $"{Math.Round(data.Item1 / divFactor, decPlaces).ToString(decPlaceString, CultureInfo.InvariantCulture)}, ";
                            result += $"{Math.Round(data.Item2 / divFactor, decPlaces).ToString(decPlaceString, CultureInfo.InvariantCulture)}";
                        }
                        else if (indices[i] == 1)
                        {
                            result += $"{Math.Round(data.Item3 / divFactor, decPlaces).ToString(decPlaceString, CultureInfo.InvariantCulture)}, ";
                            result += $"{Math.Round(data.Item4 / divFactor, decPlaces).ToString(decPlaceString, CultureInfo.InvariantCulture)}";
                        }
                        else if (indices[i] == 2)
                        {
                            result += $"{Math.Round(data.Item5 / divFactor, decPlaces).ToString(decPlaceString, CultureInfo.InvariantCulture)}, ";
                            result += $"{Math.Round(data.Item6 / divFactor, decPlaces).ToString(decPlaceString, CultureInfo.InvariantCulture)}";
                        }
                        else if (indices[i] == 3)
                        {
                            result += $"{Math.Round(data.Item7 / divFactor, decPlaces).ToString(decPlaceString, CultureInfo.InvariantCulture)}, ";
                            result += $"{Math.Round(data.Item8 / divFactor, decPlaces).ToString(decPlaceString, CultureInfo.InvariantCulture)}";
                        }
                        else if (indices[i] == 4)
                        {
                            result += $"{Math.Round(data.Item9 / divFactor, decPlaces).ToString(decPlaceString, CultureInfo.InvariantCulture)}, ";
                            result += $"{Math.Round(data.Item10 / divFactor, decPlaces).ToString(decPlaceString, CultureInfo.InvariantCulture)}";
                        }
                        else if (indices[i] == 5)
                        {
                            result += $"{Math.Round(data.Item11 / divFactor, decPlaces).ToString(decPlaceString, CultureInfo.InvariantCulture)}, ";
                            result += $"{Math.Round(data.Item12 / divFactor, decPlaces).ToString(decPlaceString, CultureInfo.InvariantCulture)}";
                        }

                        if (i != indices.Count - 1)
                        {
                            result += ", ";
                        }
                    }
                }

                return result;
            }
        }
    }
}
