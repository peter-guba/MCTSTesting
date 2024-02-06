using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessTesting.Core.AI.TwoCombos
{
    // A combination of Sigmoid and UCB1-Tuned MCTS.
    class Sigmoid_UCBTuned_MCTS : BasicMCTS
    {
        /// <summary>
        /// A parameter of the sigmoid function.
        /// </summary>
        private float k;

        public Sigmoid_UCBTuned_MCTS(int maxNumOfPlayouts, int playoutDepthLimit, string name, string bmrkID, float k) : base(maxNumOfPlayouts, playoutDepthLimit, name, bmrkID)
        {
            this.k = k;
        }

        protected override void Backpropagate(MCTSNode node, float val)
        {
            node.Value += val;
            // Scale the value to between 0 and 1 before squaring it.
            node.SquaredValueSum += (1.0f + val) * (1.0f + val) / 4;

            ++node.Visits;

            if (node.parent != null)
            {
                Backpropagate(node.parent, -val);
            }
        }

        protected override MCTSNode SelectAndExpand(MCTSNode start)
        {
            // If the current node has already been fully expanded, pick one of its
            // children using the UCB policy.
            if (start.FullyExpanded)
            {
                // If the node has no children it means its terminal, so it is returned.
                if (start.isTerminal)
                {
                    return start;
                }

                // Pick the best child according to UCB and call SelectAndExpand
                // on it.
                MCTSNode bestChild = SelectBestChild(start, UCBTuned);

                return SelectAndExpand(bestChild);
            }
            // Otherwise return this node.
            else
            {
                return start.Expand();
            }
        }

        /// <summary>
        /// Like UCB, but with a tighter bound on the uncertainty of observations.
        /// </summary>
        /// <param name="redundant"> This parameter isn't used in the function, but it
        /// must be present so that it has the same parameters as UCB. </param>
        private float UCBTuned(MCTSNode node, bool redundant)
        {
            // Scale the exploitation value to between 0 and 1.
            float exploit = (1.0f + node.Value / node.Visits) / 2;
            double v = node.SquaredValueSum / node.Visits - exploit * exploit + Math.Sqrt(2 * Math.Log(node.parent.Visits) / node.Visits);
            double explore = Math.Sqrt((Math.Log(node.parent.Visits) / node.Visits) * Math.Min(0.25, v));
            return (float)(exploit + explore);
        }

        protected override float EvaluateGame(Board b, bool team)
        {
            var moves = moveGenerator.GenerateMoves(b);
            var eval = evaluation.EvalBoard(b, !team, moves.Count == 0 && moveGenerator.InCheck());

            // Apply the sigmoid function and scale it to between -1 and 1.
            float result = (float)(1.0 / (1.0 + Math.Exp(-1.0 * k * eval))) * 2 - 1;
            return result;
        }

        public override string ToString()
        {
            return $"Sigmoid + UCB1-Tuned MCTS k: {k}, playouts: {maxNumOfPlayouts}";
        }
    }
}
