using System;

namespace ChessTesting
{
    class UCBTunedMCTS : BasicMCTS
    {
        public UCBTunedMCTS(int maxNumOfPlayouts, int playoutDepthLimit, string name, string bmrkID) : base(maxNumOfPlayouts, playoutDepthLimit, name, bmrkID)
        { }

        // Re-implemented so that SquaredValueSum gets updated while backpropagating.
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

        // Overriden so that UCB1-Tuned is used as the tree policy instead of normal UCB.
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

        public override string ToString()
        {
            return $"UCB1-Tuned MCTS, playouts: {maxNumOfPlayouts}";
        }
    }
}
