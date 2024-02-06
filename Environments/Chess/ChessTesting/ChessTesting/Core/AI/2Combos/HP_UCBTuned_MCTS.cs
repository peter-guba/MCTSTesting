using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessTesting.Core.AI.TwoCombos
{
    // A combination of MCTS HP and UCB1-Tuned MCTS.
    class HP_UCBTuned_MCTS : BasicMCTS
    {
        public HP_UCBTuned_MCTS(int maxNumOfPlayouts, int playoutDepthLimit, string name, string bmrkID) : base(maxNumOfPlayouts, playoutDepthLimit, name, bmrkID)
        { }

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

        protected override void Backpropagate(MCTSNode node, float val)
        {
            int colorIndex = node.State.WhiteToMove ? 0 : 1;
            evaluation.board = node.State;

            if (val > 0)
            {
                node.Value += val / (evaluation.CountMaterial(1 - colorIndex) + Evaluation.kingValue);
            }
            else
            {
                node.Value += val / (evaluation.CountMaterial(colorIndex) + Evaluation.kingValue);
            }

            // Scale the value to between 0 and 1 before squaring it.
            if (val > 0)
            {
                node.SquaredValueSum += (1.0f + val / (evaluation.CountMaterial(1 - colorIndex) + Evaluation.kingValue)) * (1.0f + val / (evaluation.CountMaterial(1 - colorIndex) + Evaluation.kingValue)) / 4;
            }
            else
            {
                node.SquaredValueSum += (1.0f + val / (evaluation.CountMaterial(colorIndex) + Evaluation.kingValue)) * (1.0f + val / (evaluation.CountMaterial(colorIndex) + Evaluation.kingValue)) / 4;
            }

            ++node.Visits;

            if (node.parent != null)
            {
                Backpropagate(node.parent, -val);
            }
        }

        protected override float EvaluateGame(Board b, bool team)
        {
            var moves = moveGenerator.GenerateMoves(b);

            return evaluation.EvalBoard(b, !team, moves.Count == 0 && moveGenerator.InCheck());
        }

        public override string ToString()
        {
            return $"HP + UCB1-Tuned MCTS, playouts: {maxNumOfPlayouts}";
        }
    }
}
