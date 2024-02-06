using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessTesting.Core.AI.TwoCombos
{
    // A combination of FAP MCTS and UCB1-Tuned MCTS.
    class FAP_UCBTuned_MCTS : BasicMCTS
    {
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

        public FAP_UCBTuned_MCTS(int maxNumOfPlayouts, int playoutDepthLimit, string name, string bmrkID, int numOfSegments, bool exponentialSegmentation, bool exponentialMultiplication) : base(maxNumOfPlayouts, playoutDepthLimit, name, bmrkID)
        {
            this.numOfSegments = numOfSegments;
            this.exponentialSegmentation = exponentialSegmentation;
            this.exponentialMultiplication = exponentialMultiplication;
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

            float value = val;
            float basicVal = val / multiplicativeFactor;
            MCTSNode currentNode = node;            
            while (currentNode != null)
            {
                currentNode.Value += value;
                currentNode.SquaredValueSum += ((1.0f + basicVal) * (1.0f + basicVal) / 4) * multiplicativeFactor;
                currentNode.Visits += multiplicativeFactor;
                value = -value;
                basicVal = -basicVal;
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

            return $"FAP UCB1-Tuned MCTS, segments: {numOfSegments}, segmentation: {segmentation}, multiplication: {multiplication}, playouts: {maxNumOfPlayouts}";
        }
    }
}
