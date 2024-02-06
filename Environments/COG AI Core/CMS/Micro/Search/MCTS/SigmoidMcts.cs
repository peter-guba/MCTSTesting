using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using CMS.ActionGenerators;
using CMS.Actions;
using CMS.GameStateEval;
using CMS.Players;
using CMS.Playout;
using System.Linq;
using System.Text;
using System.Xml;
using QuickGraph;
using QuickGraph.Serialization;

namespace CMS.Micro.Search.MCTS
{
    /// <summary>
    /// A variant that combines the final score and win-or-use approaches when evaluating a playout
    /// by using the sigmoid function.
    /// </summary>
    public class SigmoidMcts : Mcts
    {
        // A constant used when computing the sigmoid function.
        // It needs to be determined experimentally.
        private double k;

        public SigmoidMcts(
            IActionGenerator actionGenerator,
            int maxPlayouts,
            List<Player> players,
            double k,
            string bmrkID,
            int playoutRoundLimit = int.MaxValue) : base(actionGenerator, maxPlayouts, players, bmrkID, playoutRoundLimit, "sigmoid_mcts")
        {
            this.k = k;
        }

        // Instead of returning 1, 0 or -1, this function returns the the difference
        // between the values of the remaining pieces of the two players run through
        // a sigmoid function.
        protected override double EvaluateGame(GameResult result, GameEnvironment environment)
        {
            var hull = environment.GameState.Units[0].Values.Sum(x => x.Hull)
                       - environment.GameState.Units[1].Values.Sum(x => x.Hull);

            return 1.0 / (1.0 + Math.Exp(-1.0 * k * hull)) * 2 - 1;
        }

        public override string ToString()
        {
            return $"Sigmoid MCTS k: {k}, playouts: {_maxPlayouts}";
        }
    }
}
