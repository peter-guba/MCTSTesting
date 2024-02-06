using System;

namespace ChessTesting
{
    class VOIMCTS : BasicMCTS
    {
        public VOIMCTS(int maxNumOfPlayouts, int playoutDepthLimit, string name, string bmrkID) : base(maxNumOfPlayouts, playoutDepthLimit, name, bmrkID)
        { }

        // Overridden so that the VOI-aware tree policy is used at the root.
        protected override MCTSNode SelectAndExpand(MCTSNode node)
        {
            while (true)
            {
                // If node is not fully expanded - generate new child node with this new action and return it.
                if (!node.FullyExpanded)
                {
                    return node.Expand();
                }
                else if (node.isTerminal)
                {
                    return node;
                }

                // Find child node with highest value and call Selection recursively.
                MCTSNode bestChild = null;

                if (node.FullyExpanded && node.Children.Count == 1)
                {
                    bestChild = node.Children[0];
                }
                else if (node.parent == null)
                {
                    MCTSNode secondBestChild = null;
                    double secondBestScore = 0.0;
                    double bestScore = 0.0;
                    foreach (MCTSNode child in node.Children)
                    {
                        double childScore = (1.0 + child.Value / child.Visits) / 2;
                        if (childScore > bestScore || bestChild == null)
                        {
                            secondBestScore = bestScore;
                            bestScore = childScore;
                            secondBestChild = bestChild;
                            bestChild = child;
                        }
                        else if (childScore > secondBestScore || secondBestChild == null)
                        {
                            secondBestChild = child;
                            secondBestScore = childScore;
                        }
                    }

                    MCTSNode childWithBestVOI = null;
                    bestScore = 0.0;
                    foreach (MCTSNode child in node.Children)
                    {
                        double childScore;
                        if (child == bestChild)
                        {
                            childScore = GetVOIBest(bestChild, secondBestChild);
                        }
                        else
                        {
                            childScore = GetVOIOther(bestChild, child);
                        }

                        if (childScore > bestScore || childWithBestVOI == null)
                        {
                            bestScore = childScore;
                            childWithBestVOI = child;
                        }
                    }

                    bestChild = childWithBestVOI;
                }
                else
                {
                    bestChild = SelectBestChild(node, UCB);
                }

                if (bestChild == null)
                    return null;

                node = bestChild;
            }
        }

        /// <summary>
        /// Computes an estimate of the VOI obtained by sampling the current best child node.
        /// </summary>
        private double GetVOIBest(MCTSNode bestChild, MCTSNode secondBestChild)
        {
            double bestChildAvgVal = (1.0 + bestChild.Value / bestChild.Visits) / 2;
            double secondBestChildAvgVal = (1.0 + secondBestChild.Value / secondBestChild.Visits) / 2;
            return secondBestChildAvgVal / (bestChild.Visits + 1) *
                Math.Exp(-2.0 * Math.Pow(bestChildAvgVal - secondBestChildAvgVal, 2.0) * bestChild.Visits);
        }

        /// <summary>
        /// Computes an estimate of the VOI obtained by a child node that isn't currently the best.
        /// </summary>
        private double GetVOIOther(MCTSNode bestChild, MCTSNode otherChild)
        {
            double bestChildAvgVal = (1.0 + bestChild.Value / bestChild.Visits) / 2;
            double otherChildAvgVal = (1.0 + otherChild.Value / otherChild.Visits) / 2;
            return (1 - bestChildAvgVal) / (otherChild.Visits + 1) *
                Math.Exp(-2.0 * Math.Pow(bestChildAvgVal - otherChildAvgVal, 2.0) * otherChild.Visits);
        }

        public override string ToString()
        {
            return $"VOI MCTS, playouts: {maxNumOfPlayouts}";
        }
    }
}