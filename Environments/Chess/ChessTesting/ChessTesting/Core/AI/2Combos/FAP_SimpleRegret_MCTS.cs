using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessTesting.Core.AI.TwoCombos
{
    // A combination of FAP MCTS and SR+CR MCTS.
    class FAP_SimpleRegret_MCTS : BasicMCTS
    {
        /// <summary>
        /// Determines whether the variant should use the epsilon-greedy policy or UCBsqrt at the root.
        /// </summary>
        private bool useEpsilonGreedy = false;

        /// <summary>
        /// If the epsilon-greedy policy is used, this is the value of the epsilon parameter.
        /// </summary>
        private float epsilon = 0.5f;
        
        /// <summary>
        /// The number of segments into which the playouts are supposed to be separated.
        /// </summary>
        private readonly int numOfSegments;

        /// <summary>
        /// Determines whether the segmentation is supposed to be exponential or linear.
        /// </summary>
        private readonly bool exponentialSegmentation;

        /// <summary>
        /// Determines whether the multiplicative factor should be computed in an exponential
        /// or linear fashion.
        /// </summary>
        private readonly bool exponentialMultiplication;

        public FAP_SimpleRegret_MCTS(int maxNumOfPlayouts, int playoutDepthLimit, string name, string bmrkID, int numOfSegments, bool exponentialSegmentation, bool exponentialMultiplication, bool uEG, float epsilon) : base(maxNumOfPlayouts, playoutDepthLimit, name, bmrkID)
        {
            this.numOfSegments = numOfSegments;
            this.exponentialSegmentation = exponentialSegmentation;
            this.exponentialMultiplication = exponentialMultiplication;

            useEpsilonGreedy = uEG;
            this.epsilon = epsilon;
        }

        // Overriden to change the tree policy at the root to either UCBSqrt or epsilon-greedy,
        // depending on the parameters.
        protected override MCTSNode SelectAndExpand(MCTSNode node)
        {
            while (true)
            {
                MCTSNode bestChild;

                if (node.parent == null && useEpsilonGreedy)
                {
                    if (node.isTerminal)
                    {
                        return node;
                    }
                    else if (node.Children.Count == 0)
                    {
                        return node.Expand();
                    }
                    else
                    {
                        bestChild = SelectChildWithEpsilonGreedy(node);

                        // If the node is a new one (meaning it was created in the SelectChildWithEpsilonGreedy
                        // function), we should return.
                        if (bestChild.Visits == 0)
                        {
                            return bestChild;
                        }
                    }
                }
                else
                {
                    // If node is not fully expanded - generate new kind node with this new action and return it
                    if (!node.FullyExpanded)
                    {
                        return node.Expand();
                    }
                    else if (node.isTerminal)
                    {
                        return node;
                    }

                    if (node.parent == null)
                    {
                        bestChild = SelectBestChild(node, UCBSqrt);
                    }
                    else
                    {
                        bestChild = SelectBestChild(node, UCB);
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
        private MCTSNode SelectChildWithEpsilonGreedy(MCTSNode node)
        {
            // Find the best child.
            double bestScore = 0.0;
            int bestChildIndex = -1;
            MCTSNode bestChild = null;
            for (int i = 0; i < node.Children.Count; ++i)
            {
                MCTSNode child = node.Children[i];
                double score = child.Value / child.Visits;
                if (bestChild == null || score > bestScore)
                {
                    bestScore = score;
                    bestChildIndex = i;
                    bestChild = child;
                }
            }

            // Roll a random number between 0 and 1. If it is smaller than epsilon,
            // return the best child.
            if (rand.NextDouble() < epsilon)
            {
                return bestChild;
            }
            // Otherwise pick a different random child.
            else
            {
                int index = rand.Next(0, node.Children.Count - 1 + node.PossibleActions.Count);

                if (index < bestChildIndex)
                {
                    return node.Children[index];
                }
                else if (index >= bestChildIndex && index < node.Children.Count - 1)
                {
                    return node.Children[index + 1];
                }
                // If the index of the random child is larger than the number of children the node
                // currently has, it must be instantiated first by calling the expand function.
                else if (index >= node.Children.Count - 1 && index < node.PossibleActions.Count + node.Children.Count - 1)
                {
                    return node.Expand(index - node.Children.Count + 1);
                }
                else
                {
                    return bestChild;
                }
            }
        }

        /// <summary>
        /// Similar to UCB, but swaps the logarithm for a square root function.
        /// </summary>
        /// <param name="redundant"> This parameter isn't used in the function, but it
        /// must be present so that it has the same parameters as UCB. </param>
        private float UCBSqrt(MCTSNode n, bool redundant)
        {
            // Scale the exploitation value between 0 and 1.
            float exploitation = (1.0f + n.Value / n.Visits) / 2;
            return exploitation + C * (float)Math.Sqrt(Math.Sqrt(n.parent.Visits) / n.Visits);
        }

        protected override float EvaluateGame(Board b, bool team)
        {
            int segmentNumber = GetSegmentNumber(playoutCounter);
            float multiplicativeFactor;

            if (exponentialMultiplication)
            {
                multiplicativeFactor = (float)Math.Pow(2, segmentNumber - 1);
            }
            else
            {
                multiplicativeFactor = segmentNumber;
            }

            var moves = moveGenerator.GenerateMoves(b);

            if (moves.Count != 0)
            {
                return 0;
            }

            if (b.WhiteToMove == team)
            {
                return multiplicativeFactor;
            }
            else
            {
                return -multiplicativeFactor;
            }
        }


        protected override void Backpropagate(MCTSNode node, float val)
        {
            int segmentNumber = GetSegmentNumber(playoutCounter);
            float multiplicativeFactor;

            if (exponentialMultiplication)
            {
                multiplicativeFactor = (float)Math.Pow(2, segmentNumber - 1);
            }
            else
            {
                multiplicativeFactor = segmentNumber;
            }

            MCTSNode currentNode = node;
            float value = val;
            while (currentNode != null)
            {
                currentNode.Value += value;
                currentNode.Visits += multiplicativeFactor;
                value = -value;
                currentNode = currentNode.parent;
            }
        }

        /// <summary>
        /// Computes the number of the segment into which the n-th playout falls.
        /// </summary>
        private int GetSegmentNumber(int n)
        {
            if (exponentialSegmentation)
            {
                double aux = Math.Pow(2, numOfSegments) - 1;
                int result = 1;

                for (int i = 1; i <= numOfSegments; ++i)
                {
                    double bound = maxNumOfPlayouts * (Math.Pow(2, i) / aux);
                    if (n < bound)
                    {
                        break;
                    }
                    else
                    {
                        ++result;
                    }
                }

                return result;
            }
            else
            {
                return ((n - 1) / (maxNumOfPlayouts / numOfSegments)) + 1;
            }
        }

        public override string ToString()
        {
            string segmentation = exponentialSegmentation ? "exp" : "lin";
            string multiplication = exponentialMultiplication ? "exp" : "lin";

            if (useEpsilonGreedy)
            {
                return $"FAP Simple Regret MCTS, metric: EpsilonGreedy, segments: {numOfSegments}, segmentation: {segmentation}, multiplication: {multiplication}, epsilon: {epsilon}, playouts: {maxNumOfPlayouts}";
            }
            else
            {
                return $"FAP Simple Regret MCTS, metric: UCTSqrt, segments: {numOfSegments}, segmentation: {segmentation}, multiplication: {multiplication}, playouts: {maxNumOfPlayouts}";
            }
        }
    }
}
