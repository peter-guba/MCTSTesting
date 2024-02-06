using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using ChessTesting;

namespace Benchmarking
{
    /// <summary>
    /// Represents a single benchmark from "Resources/Benchmarks"
    /// </summary>
    internal class Benchmark
    {
        private static Random rand = new Random();

        private readonly List<BattleSettings> _battles;
        private readonly List<ISearch> _searches;
        private readonly int _roundMax;
        private readonly bool _isSymmetric;
        private readonly string _name;
        private readonly GameManager gm;

        /// <summary>
        /// Creates a benchmark with given parameters.
        /// </summary>
        /// <param name="battles">Battles to be run in this benchmark.</param>
        /// <param name="players">Players which battle in this benchmark.</param>
        /// <param name="roundMax">Maximum number of rounds for each batte.</param>
        /// <param name="isSymmetric">Should the players be switched and play again?</param>
        /// <param name="name">File name of the benchmark.</param>
        /// <param name="playerNames">Names of the players.</param>
        public Benchmark(List<BattleSettings> battles, List<ISearch> searches, int roundMax, bool isSymmetric, string name)
        {
            _battles = battles;
            _searches = searches;
            _roundMax = roundMax;
            _isSymmetric = isSymmetric;
            _name = name;
            
            gm = new GameManager();
        }

        /// <summary>
        /// Starts this benchmark.
        /// </summary>
        /// <param name="resultDir">Where should the results be outputted.</param>
        public void Run(string resultDir)
        {
            Console.WriteLine("Benchmark started");
            Console.WriteLine($"{_searches[0]}");
            Console.WriteLine($"{_searches[1]}");

            var sumResults = new Results();

            Stopwatch totalTime = Stopwatch.StartNew();
            int battleNumber = 1;
            bool prevUnfinished = false;
            int prevWinner = -2;

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

                    // Although battle names are inconsequential in chess, as every chess game
                    // starts with a regularly set chess board, they are still printed out in order
                    // to make the outputs consistent with the other testing environments.
                    Console.WriteLine($"Battle {battle.Name} started for {battle.Repeats} repeats");
                    _searches[0].SetBattleName(battle.Name);
                    _searches[1].SetBattleName(battle.Name);

                    // Run the battle the given number of times.
                    for (int r = 0; r < battle.Repeats; r++)
                    {
                        // The final score of both players at the end of the previous game.
                        // Used when deciding symwins.
                        var prevPiecePoints = new[] { 0.0f, 0.0f };

                        // Run the battle for two iterations while switching the player that gets
                        // to go first.
                        for (int iter = 0; iter < 2; iter++)
                        {
                            Console.WriteLine($"  Iter number {battleNumber++} started");

                            string rs = rand.Next(100000, 1000000).ToString();
                            _searches[0].SetRndBattleString(rs);
                            _searches[1].SetRndBattleString(rs);

                            // Set up a new game.
                            if (iter == 0)
                            {
                                gm.NewGame(_searches[0], _searches[1]);
                            }
                            else
                            {
                                gm.NewGame(_searches[1], _searches[0]);
                            }

                            // Run the simulation.
                            GameResult result;
                            if (iter == 0)
                            {
                                result = gm.Simulate(0, _roundMax);
                            }
                            else
                            {
                                result = gm.Simulate(1, _roundMax);
                            }

                            results.RoundCounts.Add(result.RoundCount);

                            int winner = result.GetWinner(_searches[0]);
                            var piecePoints = new[] { 0.0f, 0.0f };

                            // Process the results. If the game didn't finish with one of the players winning,
                            // designate it as unfinished and process the results accordingly.
                            if (result.IsFinished())
                            {
                                results.WinCounts[winner]++;
                                results.PiecePointsRemaining[winner] += result.RemainingPiecePointsWinner;
                                piecePoints[winner] = result.RemainingPiecePointsWinner;
                                results.PiecePointsRemaining[1 - winner] += result.RemainingPiecePointsLoser;
                                piecePoints[1 - winner] = result.RemainingPiecePointsLoser;
                                Console.WriteLine($"    Winner: {result.Winner} {_searches[winner]}");
                            }
                            else
                            {
                                results.Unfinished++;

                                results.PiecePointsRemaining[0] += result.RemainingPiecePointsWinner;
                                piecePoints[0] = result.RemainingPiecePointsWinner;
                                results.PiecePointsRemaining[1] += result.RemainingPiecePointsLoser;
                                piecePoints[1] = result.RemainingPiecePointsLoser;
                            }

                            for (int i = 0; i < _searches.Count; ++i)
                            {
                                results.RoundTimes[i].AddRange(result.RoundTimes[i]);
                            }

                            if (!_isSymmetric)
                            {
                                break;
                            }
                            // If this is the second iteration, decide who the symwinner is.
                            else if (iter == 1)
                            {
                                // If both games were unfinished, the winner is decided based on the difference
                                // between the remaining piece points of the two players at the end of the
                                // two games.
                                if (winner == -2 && prevUnfinished)
                                {
                                    var symWinner = piecePoints[0] + prevPiecePoints[0] > piecePoints[1] + prevPiecePoints[1] ? 0 : piecePoints[0] + prevPiecePoints[0] < piecePoints[1] + prevPiecePoints[1] ? 1 : 2;
                                    results.SymWinCounts[symWinner]++;
                                }
                                // If only the second game was unfinished, the symwin goes to the winner
                                // of the first.
                                else if (winner == -2)
                                {
                                    var symWinner = prevWinner;
                                    results.SymWinCounts[symWinner]++;
                                }
                                // If only the first game was unfinished, the symwin goes to the winner
                                // of the second.
                                else if (prevUnfinished)
                                {
                                    var symWinner = winner;
                                    results.SymWinCounts[symWinner]++;
                                }
                                // If both games were won by the same player, the symwin goes to that player.
                                else if (winner == prevWinner)
                                {
                                    var symWinner = winner;
                                    results.SymWinCounts[symWinner]++;
                                }
                                // If the games were won by two different players, the symwin goes to the one
                                // that ended with more pieces remaining.
                                else
                                {
                                    var symWinner = piecePoints[winner] > prevPiecePoints[prevWinner] ? winner : piecePoints[winner] < prevPiecePoints[prevWinner] ? 1 - winner : 2;
                                    results.SymWinCounts[symWinner]++;
                                }
                            }
                            else
                            {
                                prevPiecePoints[0] = piecePoints[0];
                                prevPiecePoints[1] = piecePoints[1];

                                prevUnfinished = winner == -2;
                                prevWinner = winner;
                            }

                            int p1Win = result.GetWinner(_searches[0]) == 0 ? 1 : 0;
                            int p2Win = result.GetWinner(_searches[1]) == 0 ? 1 : 0;

                            OutputResults(resultsWriter, battle, p1Win, p2Win, piecePoints[0], piecePoints[1], result.RoundCount);
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
            var sumPiecePointsResults = results.PiecePointsRemaining;
            Console.WriteLine("Benchmark ended");
            Console.WriteLine($"{sumResults[0]} wins, {sumSymetricResults[0]} symWins, {sumPiecePointsResults[0]} hull {sumPiecePointsResults[0]}");
            Console.WriteLine($"{sumResults[1]} wins, {sumSymetricResults[1]} symWins, {sumPiecePointsResults[1]} hull {sumPiecePointsResults[1]}");
            if (sumSymetricResults[2] != 0)
                Console.WriteLine($"SymDraws: {sumSymetricResults[2]}");
            if (results.Unfinished > 0)
            {
                Console.WriteLine($"Unfinished: {results.Unfinished}");
            }
        }

        /// <summary>
        /// Prints results using the given streamwriter.
        /// </summary>
        private void OutputResults(StreamWriter sw, BattleSettings battle, int p1Win, int p2Win, float p1ppRemaining, float p2ppRemaining, int roundCount)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            sw.WriteLine(string.Join(";",
                battle.Name,
                p1Win,
                p2Win,
                p1ppRemaining,
                p2ppRemaining,
                roundCount
            ));
        }
    }
}