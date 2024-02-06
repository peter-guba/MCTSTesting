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
    /// A variant that tries to minimize simple regret at the root by estimating the value of information obtained
    /// from playouts.
    /// </summary>
    public class VoiMcts : Mcts
    {
        public VoiMcts(
            IActionGenerator actionGenerator,
            int maxPlayouts,
            List<Player> players,
            string bmrkID,
            int playoutRoundLimit = int.MaxValue,
            string name = "voi_mcts") : base(actionGenerator, maxPlayouts, players, bmrkID, playoutRoundLimit, name)
        {}

        // Overridden so that the VOI-aware tree policy is used at the root.
        protected override MctsNode SelectAndExpand(MctsNode node)
        {
            Debug.Assert(node != null);

            while (true)
            {
                // If node is not fully expanded - generate new child node with this new action and return it.
                MctsNode newChild;
                if (node.TryGetNextChild(_actionGenerator, out newChild))
                {
                    return newChild;
                }
                else if (node.IsTerminal)
                {
                    return node;
                }

                // Find child node with highest value and call Selection recursively.
                int player = node.CurrentActionState.Environment.GameState.ActivePlayer;
                MctsNode bestChild = null;

                if (node.Children.Count == 1)
                {
                    bestChild = node.Children[0];
                }
                else if (node.Parent == null)
                {
                    MctsNode secondBestChild = null;
                    double secondBestScore = 0.0;
                    double bestScore = 0.0;
                    foreach (MctsNode child in node.Children)
                    {
                        double childScore = player == 0 ? (1.0 + child.Value / child.VisitedCount) / 2 : (1.0 - child.Value / child.VisitedCount) / 2;
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

                    MctsNode childWithBestVOI = null;
                    bestScore = 0.0;
                    foreach (MctsNode child in node.Children)
                    {
                        double childScore;
                        if (child == bestChild) {
                            childScore = GetVOIBest(bestChild, secondBestChild, player);
                        }
                        else
                        {
                            childScore = GetVOIOther(bestChild, child, player);
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
                    bestChild = SelectBestChild(node, player, Ucb);
                }

                if (bestChild == null)
                    return null;

                node = bestChild;
            }
        }

        /// <summary>
        /// Computes an estimate of the VOI obtained by sampling the current best child node.
        /// </summary>
        private double GetVOIBest(MctsNode bestChild, MctsNode secondBestChild, int player)
        {
            double bestChildAvgVal = player == 0 ? (1.0 + bestChild.Value / bestChild.VisitedCount) / 2 : (1.0 - bestChild.Value / bestChild.VisitedCount) / 2;
            double secondBestChildAvgVal = player == 0 ? (1.0 + secondBestChild.Value / secondBestChild.VisitedCount) / 2 : (1.0 - secondBestChild.Value / secondBestChild.VisitedCount) / 2;
            return secondBestChildAvgVal / (bestChild.VisitedCount + 1) *
                Math.Exp(-2.0 * Math.Pow(bestChildAvgVal - secondBestChildAvgVal, 2.0) * bestChild.VisitedCount);
        }

        /// <summary>
        /// Computes an estimate of the VOI obtained by a child node that isn't currently the best.
        /// </summary>
        private double GetVOIOther(MctsNode bestChild, MctsNode otherChild, int player)
        {
            double bestChildAvgVal = player == 0 ? (1.0 + bestChild.Value / bestChild.VisitedCount) / 2 : (1.0 - bestChild.Value / bestChild.VisitedCount) / 2;
            double otherChildAvgVal = player == 0 ? (1.0 + otherChild.Value / otherChild.VisitedCount) / 2 : (1.0 - otherChild.Value / otherChild.VisitedCount) / 2;
            return (1 - bestChildAvgVal) / (otherChild.VisitedCount + 1) *
                Math.Exp(-2.0 * Math.Pow(bestChildAvgVal - otherChildAvgVal, 2.0) * otherChild.VisitedCount);
        }

        public override string ToString()
        {
            return $"VOI MCTS, playouts: {_maxPlayouts}";
        }
    }
}
