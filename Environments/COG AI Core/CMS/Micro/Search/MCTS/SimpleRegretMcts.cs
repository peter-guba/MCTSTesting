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

namespace CMS.Micro.Search.MCTS
{
    /// <summary>
    /// A variant that tries to minimize simple regret at the root by using a different
    /// tree policy than UCB1.
    /// </summary>
    public class SimpleRegretMcts : Mcts
    {
        // Determines whether the variant should use the epsilon-greedy policy or UCBsqrt at the root.
        private bool useEpsilonGreedy = false;

        private double epsilon = 0.5;

        private Random r = new Random(Guid.NewGuid().GetHashCode());

        public SimpleRegretMcts(
            IActionGenerator actionGenerator,
            int maxPlayouts,
            List<Player> players,
            bool useEpsilonGreedy,
            string bmrkID,
            double epsilon,
            int playoutRoundLimit = int.MaxValue) : base(actionGenerator, maxPlayouts, players, bmrkID, playoutRoundLimit, "simple_regret_mcts")
        {
            this.useEpsilonGreedy = useEpsilonGreedy;
            this.epsilon = epsilon;
        }

        // Overriden to change the tree policy at the root to either UCBSqrt or epsilon-greedy,
        // depending on the parameters.
        protected override MctsNode SelectAndExpand(MctsNode node)
        {
            Debug.Assert(node != null);

            while (true)
            {
                MctsNode bestChild = null;
                int player = node.CurrentActionState.Environment.GameState.ActivePlayer;

                if (node.Parent == null && useEpsilonGreedy)
                {
                    bestChild = SelectChildWithEpsilonGreedy(node, player);

                    // If the node is a new one (meaning it was created in the SelectChildWithEpsilonGreedy
                    // function), we should return.
                    if (bestChild.VisitedCount == 0)
                    {
                        return bestChild;
                    }
                }
                else
                {
                    // If node is not fully expanded - generate new kind node with this new action and return it
                    MctsNode newChild;
                    if (node.TryGetNextChild(_actionGenerator, out newChild))
                    {
                        return newChild;
                    }
                    else if (node.IsTerminal)
                    {
                        return node;
                    }

                    if (node.Parent == null)
                    {
                        bestChild = SelectBestChild(node, player, UcbSqrt);
                    }
                    else
                    {
                        bestChild = SelectBestChild(node, player, Ucb);
                    }
                }

                if (bestChild == null)
                    return null;

                node = bestChild;
            }
        }

        /// <summary>
        /// Implementation of a tree policy that picks the best child with probability
        /// epsilon and a random different child with probability 1 - epsilon.
        /// </summary>
        protected MctsNode SelectChildWithEpsilonGreedy(MctsNode node, int player)
        {
            // Find the best child.
            double bestScore = 0.0;
            int bestChildIndex = -1;
            MctsNode bestChild = null;
            for (int i = 0; i < node.Children.Count; ++i)
            {
                MctsNode child = node.Children[i];

                if (child.VisitedCount != 0)
                {
                    double score = child.Value / child.VisitedCount;
                    if (bestChild == null ||
                        (player == 0 && score > bestScore) ||
                        (player == 1 && score < bestScore))
                    {
                        bestScore = score;
                        bestChildIndex = i;
                        bestChild = child;
                    }
                }
            }

            // Roll a random number between 0 and 1. If it is smaller than epsilon,
            // return the best child.
            if (bestChild != null && r.NextDouble() < epsilon)
            {
                return bestChild;
            }
            else
            {
                int index = r.Next(0, _actionGenerator.GetActionCount(node.CurrentActionState.Environment) - 1);

                if (index < bestChildIndex)
                {
                    return node.Children[index];
                }
                else if (index >= bestChildIndex && index < node.Children.Count - 1)
                {
                    return node.Children[index + 1];
                }
                else {
                    MctsNode newNode;
                    if (node.TryGetNextChild(_actionGenerator, out newNode))
                    {
                        return newNode;
                    }
                    else
                    {
                        return bestChild;
                    }
                }
            }
        }

        /// <summary>
        /// Similar to UCB, but swaps the logarithm for a square root function.
        /// </summary>
        /// <param name="redundant"> This parameter isn't used in the function, but it
        /// must be present so that it has the same parameters as UCB. </param>
        internal static double UcbSqrt(MctsNode node, int player, bool redundant)
        {
            // Scale the exploitation value between 0 and 1.
            double exploit = player == 0 ? (1.0 + node.Value / node.VisitedCount) / 2 : (1.0 - node.Value / node.VisitedCount) / 2;
            double explore = C * Math.Sqrt(Math.Sqrt(node.Parent.VisitedCount) / node.VisitedCount);
            return explore + exploit;
        }

        public override string ToString()
        {
            if (useEpsilonGreedy)
            {
                return $"Simple Regret MCTS, metric: EpsilonGreedy, epsilon: {epsilon}, playouts: {_maxPlayouts}";
            }
            else
            {
                return $"Simple Regret MCTS, metric: UCTSqrt, playouts: {_maxPlayouts}";
            }
        }
    }
}
