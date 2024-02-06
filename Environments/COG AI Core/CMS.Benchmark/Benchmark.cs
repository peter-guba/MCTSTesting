using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CMS.Players;
using CMS.Playout;
using System.Configuration;
using CMS.Pathfinding;
using CMS.Units;

namespace CMS.Benchmark
{
    /// <summary>
    /// Represents a single benchmark from "Resources/Benchmarks"
    /// </summary>
    internal class Benchmark
    {
        private static Random rand = new Random();

        private readonly List<BattleSettings> _battles;
        private readonly List<Player> _players;
        private readonly int _roundMax;
        private readonly bool _isSymmetric;
        private readonly string _name;

        /// <summary>
        /// Creates a benchmark with given parameters.
        /// </summary>
        /// <param name="battles">Battles to be run in this benchmark.</param>
        /// <param name="players">Players which battle in this benchmark.</param>
        /// <param name="roundMax">Maximum number of rounds for each batte.</param>
        /// <param name="isSymmetric">Should the players be switched and play again?</param>
        /// <param name="name">File name of the benchmark.</param>
        /// <param name="playerNames">Names of the players.</param>
        public Benchmark(List<BattleSettings> battles, List<Player> players, int roundMax, bool isSymmetric, string name)
        {
            _battles = battles;
            _players = players;
            _roundMax = roundMax;
            _isSymmetric = isSymmetric;
            _name = name;
        }

        /// <summary>
        /// Starts this benchmark.
        /// </summary>
        /// <param name="resultDir">Where should the results be outputted.</param>
        public void Run(string resultDir)
        {
            Console.WriteLine("Benchmark started");
            Console.WriteLine($"{_players[0]}");
            Console.WriteLine($"{_players[1]}");

            var sumResults = new Results();

            Stopwatch totalTime = Stopwatch.StartNew();
            int battleNumber = 1;
            bool prevUnfinishedOrDraw = false;

            var resultsFile = Path.Combine(resultDir, $"{_name}.csv");
            if (File.Exists(resultsFile))
                Console.WriteLine($"[WARNING] File {resultsFile} already exists, overwriting");

            using (var resultsWriter = new StreamWriter(resultsFile))
            {
                resultsWriter.WriteLine(
                    "battleName; p1Win; p2Win; p1hull; p2hull; rounds;"
                );

                // Run every battle specified in the benchmark.
                foreach (BattleSettings battle in _battles)
                {
                    var results = new Results();

                    _players[0].SetBattleName(battle.Name);
                    _players[1].SetBattleName(battle.Name);

                    Console.WriteLine($"Battle {battle.Name} started for {battle.Repeats} repeats");

                    // Run the battle for the given amount of repeats.
                    for (int r = 0; r < battle.Repeats; r++)
                    {
                        var hulls = new[] { 0.0f, 0.0f };
                        float[] hullsSum = new float[] { 0.0f, 0.0f };

                        // Run each battle for two iterations while swapping who the first player is.
                        for (int iter = 0; iter < 2; iter++)
                        {
                            Console.WriteLine($"  Iter number {battleNumber++} started");
                            GameEnvironment envi = battle.GameEnvironment.DeepCloneState();

                            string rs = rand.Next(100000, 1000000).ToString();
                            _players[0].SetRndBattleString(rs);
                            _players[1].SetRndBattleString(rs);

                            // Swap the two players.
                            if (iter == 1)
                            {
                                SwapUnitsAndActive(envi);
                            }

                            GameResult result = Game.Playout(envi, _players, _roundMax, detailedStats: true);

                            results.RoundCounts.Add(result.RoundCount);
                            int winner = result.Winner;

                            if (!result.IsDraw)
                            {
                                // If the result wasn't a draw but neither player won, it means the game
                                // didn't finish in the allotted number rounds.
                                if (result.Winner == -2)
                                {
                                    results.Unfinished++;

                                    float h0 = envi.GameState.Units[0].Values.Sum(u => u.Hull); ;
                                    float h1 = envi.GameState.Units[1].Values.Sum(u => u.Hull); ;

                                    hullsSum[0] += h0;
                                    hullsSum[1] += h1;

                                    winner = h0 > h1 ? 0 : h1 > h0 ? 1 : 2;

                                    results.WinCounts[winner]++;
                                    results.HullRemaining[0] += h0;
                                    results.HullRemaining[1] += h1;
                                }
                                else
                                {
                                    results.WinCounts[result.Winner]++;
                                    float totalHull = envi.GameState.Units[result.Winner].Values.Sum(u => u.Hull);
                                    results.HullRemaining[result.Winner] += totalHull;
                                    hulls[result.Winner] = totalHull;
                                    hullsSum[result.Winner] += totalHull;
                                    Console.WriteLine($"    Winner: {result.Winner} {_players[result.Winner]}");
                                }
                            }
                            else
                            {
                                results.WinCounts[2]++;
                            }

                            for (int i = 0; i < _players.Count; ++i)
                            {
                                results.RoundTimes[i].AddRange(result.RoundTimes[i]);
                            }

                            if (!_isSymmetric)
                            {
                                break;
                            }
                            // If this is the second iteration, decide who the symwin goes to.
                            else if (iter == 1)
                            {
                                // If both this game and the previous one had no winner,
                                // i.e. were unfinished or drawn, then who the symwin goes to
                                // is decided based on the sum of the players' hps from both
                                // games.
                                if (prevUnfinishedOrDraw && (result.IsDraw || result.Winner == -2))
                                {                                    
                                    int symwinner = hullsSum[0] > hullsSum[1] ? 0 : hullsSum[0] < hullsSum[1] ? 1 : 2;                                    
                                    results.SymWinCounts[symwinner]++;
                                }
                                // Otherwise, there are three possibilities.
                                // 1. Only one of the games had a winner, in which case
                                // only one of the fields in hps has a non-zero value.
                                // 2. Both games were won by the same player, in which case
                                // again, one of the fields is zero.
                                // 3. The two games were won by two different players,
                                // in which case both fields in hps are non-zero and the
                                // symwin goes to the player with more remaining hit points.
                                else
                                {
                                    int symwinner = hulls[0] > hulls[1] ? 0 : hulls[0] < hulls[1] ? 1 : 2;
                                    results.SymWinCounts[symwinner]++;
                                }
                            }

                            prevUnfinishedOrDraw = result.IsDraw || result.Winner == -2;

                            int p1Win = winner == 0 ? 1 : 0;
                            int p2Win = winner == 1 ? 1 : 0;

                            OutputResults(resultsWriter, battle, p1Win, p2Win, envi.GameState.Units[0].Values.Sum(u => u.Hull), envi.GameState.Units[1].Values.Sum(u => u.Hull), result.RoundCount);
                        }
                    }
                    Console.WriteLine("Battle ended");
                    sumResults.Add(results);
                }
            }
            OutputFinalBenchmarkResults(sumResults);
            Console.WriteLine($"Total benchmark time: {totalTime.Elapsed}\n");
        }

        /// <summary>
        /// Prints out the final results to the console. Used at the end of the benchmark.
        /// </summary>
        private void OutputFinalBenchmarkResults(Results results)
        {
            var sumResults = results.WinCounts;
            var sumSymetricResults = results.SymWinCounts;
            var sumHullResults = results.HullRemaining;
            Console.WriteLine("Benchmark ended");
            Console.WriteLine($"{sumResults[0]} wins, {sumSymetricResults[0]} symWins, {sumHullResults[0]} hull");
            Console.WriteLine($"{sumResults[1]} wins, {sumSymetricResults[1]} symWins, {sumHullResults[1]} hull");
            if (sumResults[2] != 0)
                Console.WriteLine($"Draws: {sumResults[2]}");
            if (sumSymetricResults[2] != 0)
                Console.WriteLine($"SymDraws: {sumSymetricResults[2]}");
            if (results.Unfinished != 0)
            {
                Console.WriteLine($"Unfinished: {results.Unfinished}");
            }
        }

        /// <summary>
        /// Switches who the first player is and the units that each player has at
        /// their disposal.
        /// </summary>
        private static void SwapUnitsAndActive(GameEnvironment envi)
        {
            var tmp = envi.GameState.Units[0];
            envi.GameState.Units[0] = envi.GameState.Units[1];
            envi.GameState.Units[1] = tmp;

            envi.GameState.ActivePlayer = GameState.Opo(envi.GameState.ActivePlayer);
        }

        /// <summary>
        /// Prints results using the given streamwriter.
        /// </summary>
        private void OutputResults(StreamWriter sw, BattleSettings battle, int p1Win, int p2Win, float p1Hull, float p2Hull, int roundCount)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            sw.WriteLine(string.Join(";",
                battle.Name,
                p1Win,
                p2Win,
                p1Hull,
                p2Hull,
                roundCount
            ));
        }
    }
}

