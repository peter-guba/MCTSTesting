using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CMS.Players;
using CMS.ActionGenerators;
using CMS.Actions;
using CMS.GameStateEval;
using CMS.Playout;
using System.Diagnostics;
using System.IO;

namespace CMS.Micro.Search.MCTS
{
    /// <summary>
    /// A variant that uses the UCB1-Tuned formula as its tree policy, instead of UCB1.
    /// </summary>
    public class UcbTunedMcts : Mcts
    {
        public UcbTunedMcts(
            IActionGenerator actionGenerator,
            int maxPlayouts,
            List<Player> players,
            string bmrkID,
            int playoutRoundLimit = int.MaxValue) : base(actionGenerator, maxPlayouts, players, bmrkID, playoutRoundLimit, "ucb_tuned_mcts")
        { }

        // Overriden in order to replace MctsNode with UctTunedMctsNode.
        internal override ICollection<ActionCms> GetActions(GameEnvironment environment)
        {
            TotalIterCount = 0;
            maxDepth = 0;
            var rootActionState = new ActionStatePair { Environment = environment };
            IEnumerable<ActionStatePair> rootChildActions = _actionGenerator.EnumerateActions(rootActionState.Environment);

            var root = new UcbTunedMctsNode(null, rootActionState, rootChildActions, 0);

            using (var sw = new StreamWriter(path + name + "/" + bmrkID + "_" + battleName + "_" + randomBattleString + ".txt", true))
            {
                while (TotalIterCount < _maxPlayouts)
                {
                    long timeStamp = Stopwatch.GetTimestamp();

                    ++TotalIterCount;
                    // Selection and Expansion
                    MctsNode selectedNode = SelectAndExpand(root);

                    // Playout
                    GameEnvironment selectedStateClone = selectedNode.CurrentActionState.Environment.DeepCloneState();
                    GameResult result = Game.Playout(selectedStateClone, _players, _playoutRoundLimit);
                    double value = EvaluateGame(result, selectedStateClone);

                    // Backpropagation
                    BackpropagateResults((UcbTunedMctsNode)selectedNode, value);

                    if (selectedNode.depth > maxDepth)
                    {
                        maxDepth = selectedNode.depth;
                    }

                    LogTimeAndDepth(1_000_000_000.0 * (Stopwatch.GetTimestamp() - timeStamp) / Stopwatch.Frequency, sw);
                }
            }

            MctsNode bestNode = SelectBestChild(root, environment.GameState.ActivePlayer, (n, p) => n.VisitedCount == 0 ? Double.NegativeInfinity : (p == 0 ? n.Value / n.VisitedCount : -n.Value / n.VisitedCount));
            var actions = bestNode.CurrentActionState.PlayerAction;

            return actions;
        }

        // Re-implemented in order to replace MctsNode with UctTunedMctsNode and Ucb with UcbTuned.
        protected MctsNode SelectAndExpand(UcbTunedMctsNode node)
        {
            Debug.Assert(node != null);
            while (true)
            {
                // If node is not fully expanded - generate new child node with this new action and return it
                UcbTunedMctsNode newChild;
                if (node.TryGetNextChild(_actionGenerator, out newChild))
                {
                    return newChild;
                }
                else if (node.IsTerminal)
                {
                    return node;
                }

                // Else find child node with highest value and call Selection recursively
                int player = node.CurrentActionState.Environment.GameState.ActivePlayer;
                UcbTunedMctsNode bestChild = SelectBestChild(node, player, UcbTuned);

                if (bestChild == null)
                    return null;

                node = bestChild;
            }
        }

        // Re-implemented in order to replace MctsNode with UctTunedMctsNode.
        protected UcbTunedMctsNode SelectBestChild(UcbTunedMctsNode node, int player, Func<UcbTunedMctsNode, int, double> evalFunc)
        {
            double bestScore = 0.0;
            UcbTunedMctsNode bestChild = null;
            foreach (UcbTunedMctsNode child in node.Children)
            {
                double score = evalFunc(child, player);
                if (bestChild == null || score > bestScore)
                {
                    bestScore = score;
                    bestChild = child;
                }
            }

            return bestChild;
        }

        // Re-implemented so that SquaredValueSum gets updated while backpropagating.
        protected void BackpropagateResults(UcbTunedMctsNode node, double value)
        {
            do
            {
                // Scale the value to between 0 and 1 before squaring it.
                node.SquaredValueSum += node.CurrentActionState.Environment.GameState.ActivePlayer == 1 ? (1.0 + value) * (1.0 + value) / 4 : (1.0 - value) * (1.0 - value) / 4;
                node.VisitedCount++;
                node.Value += value;
                node = (UcbTunedMctsNode)node.Parent;
            } while (node != null);
        }

        /// <summary>
        /// Like UCB, but with a tighter bound on the uncertainty of observations.
        /// </summary>
        /// <param name="redundant"> This parameter isn't used in the function, but it
        /// must be present so that it has the same parameters as UCB. </param>
        internal static double UcbTuned(UcbTunedMctsNode node, int player)
        {
            // Scale the exploitation value between 0 and 1.
            double exploit = player == 0 ? (1.0 + node.Value / node.VisitedCount) / 2 : (1.0 - node.Value / node.VisitedCount) / 2;
            double v = node.SquaredValueSum / node.VisitedCount - exploit * exploit + Math.Sqrt(2 * Math.Log(node.Parent.VisitedCount) / node.VisitedCount);
            double explore = Math.Sqrt((Math.Log(node.Parent.VisitedCount) / node.VisitedCount) * Math.Min(0.25, v));
            return exploit + explore;
        }

        public override string ToString()
        {
            return $"UCB1-Tuned MCTS, playouts: {_maxPlayouts}";
        }
    }
}