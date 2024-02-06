using System;
using Benchmarking;
using System.Globalization;
using System.Threading;
using System.Collections.Generic;
using static ChessTesting.GameManager;
using static System.Net.Mime.MediaTypeNames;
using System.Diagnostics.Metrics;

namespace ChessTesting
{
    class Program
    {
        // Runs the tests.
        static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            if (args.Length != 1)
            {
                RunCheckmateTests(args);
            }
            else
            {
                string benchmarkSet = args[0];

                // Run the given benchmark set.
                var br = new BenchmarkRunner();
                br.Run(benchmarkSet);
            }
        }

        /// <summary>
        /// Runs the checkmate tests.
        /// </summary>
        private static void RunCheckmateTests(string[] args)
        {
            GameManager gm = new GameManager();

            int aiIndex = int.Parse(args[0]);
            int testIndex = int.Parse(args[1]);
            int repeats = 10;

            if (args.Length > 2)
            {
                repeats = int.Parse(args[2]);
            }

            int maxIterations = 10000000;
            int playoutLength = 100;

            BasicMCTS mcts;
            
            switch (aiIndex)
            {
                case 0: mcts = new BasicMCTS(maxIterations, playoutLength, "basic_mcts", "checkmate_tests"); break;
                case 1: mcts = new FAPMCTS(maxIterations, playoutLength, "fap_mcts", "checkmate_tests", 100, true, false); break;
                case 2: mcts = new MCTSHP(maxIterations, playoutLength, "mcts_hp", "checkmate_tests"); break;
                case 3: mcts = new RQBonusMCTS(maxIterations, playoutLength, "q_bonus_mcts", "checkmate_tests", 100.0f, false, true); break;
                case 4: mcts = new RQBonusMCTS(maxIterations, playoutLength, "r_bonus_mcts", "checkmate_tests", 10.0f, true, false); break;
                case 5: mcts = new RQBonusMCTS(maxIterations, playoutLength, "rq_bonus_mcts", "checkmate_tests", 1.0f, true, true); break;
                case 6: mcts = new SigmoidMCTS(maxIterations, playoutLength, "sigmoid_mcts", "checkmate_tests", 1.0f); break;
                case 7: mcts = new SimpleRegretMCTS(maxIterations, playoutLength, "sr_cr_mcts", "checkmate_tests", true, 0.75f); break;
                case 8: mcts = new UCBTunedMCTS(maxIterations, playoutLength, "ucb_tuned_mcts", "checkmate_tests"); break;
                case 9: mcts = new VOIMCTS(maxIterations, playoutLength, "voi_mcts", "checkmate_tests"); break;
                case 10: mcts = new WPMCTS(maxIterations, playoutLength, "wp_mcts", "checkmate_tests", 10.0f, 10.0f, false); break;
                default: throw new Exception("The number you entered is too damn high!");
            }

            gm.RunCheckmateTests(repeats, testIndex, mcts);
        }
    }
}
