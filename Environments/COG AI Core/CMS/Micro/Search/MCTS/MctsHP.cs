using System.Collections.Generic;
using CMS.ActionGenerators;
using CMS.Players;
using CMS.Playout;
using System.Linq;

namespace CMS.Micro.Search.MCTS
{
    public class MctsHP : Mcts
    {
        public MctsHP(
            IActionGenerator actionGenerator,
            int maxPlayouts,
            List<Player> players,
            string bmrkID,
            int playoutRoundLimit = int.MaxValue,
            string name = "mcts_hp") : base(actionGenerator, maxPlayouts, players, bmrkID, playoutRoundLimit, name)
        { }

        // The main change with respect to basic MCTS is this function.
        // Instead of just backpropagating the value obtained from evaluating
        // the final state of the playout, it is normalised at every node.
        protected override void BackpropagateResults(MctsNode node, double value)
        {
            do
            {
                node.VisitedCount++;

                if (value < 0)
                    node.Value += value / node.UnitHulls[1];
                else
                    node.Value += value / node.UnitHulls[0];

                node = node.Parent;
            } while (node != null);
        }

        // Instead of returning 1, 0 or -1, this function returns the difference between
        // the values of the remaining pieces of the two players.
        protected override double EvaluateGame(GameResult result, GameEnvironment environment)
        {
            var hull = environment.GameState.Units[0].Values.Sum(x => x.Hull)
                       - environment.GameState.Units[1].Values.Sum(x => x.Hull);
            return hull;
        }

        public override string ToString()
        {
            return $"MCTS HP, playouts: {_maxPlayouts}";
        }
    }
}
