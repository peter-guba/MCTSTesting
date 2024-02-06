using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessTesting.Core.AI.TwoCombos
{
    // A combination of FAP MCTS and Sigmoid MCTS.
    class FAP_Sigmoid_MCTS : BasicMCTS
    {
        /// <summary>
        /// A parameter of the sigmoid function.
        /// </summary>
        private float k;

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

        public FAP_Sigmoid_MCTS(int maxNumOfPlayouts, int playoutDepthLimit, string name, string bmrkID, int numOfSegments, bool exponentialSegmentation, bool exponentialMultiplication, float k) : base(maxNumOfPlayouts, playoutDepthLimit, name, bmrkID)
        {
            this.numOfSegments = numOfSegments;
            this.exponentialSegmentation = exponentialSegmentation;
            this.exponentialMultiplication = exponentialMultiplication;

            this.k = k;
        }

        protected override float EvaluateGame(Board b, bool team)
        {
            var moves = moveGenerator.GenerateMoves(b);
            var eval = evaluation.EvalBoard(b, !team, moves.Count == 0 && moveGenerator.InCheck());

            // Apply the sigmoid function and scale it to between -1 and 1.
            float result = (float)(1.0 / (1.0 + Math.Exp(-1.0 * k * eval))) * 2 - 1;

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

            return result * multiplicativeFactor;
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

            return $"FAP Sigmoid MCTS, k: {k}, segments: {numOfSegments}, segmentation: {segmentation}, multiplication: {multiplication}, playouts: {maxNumOfPlayouts}";
        }
    }
}
