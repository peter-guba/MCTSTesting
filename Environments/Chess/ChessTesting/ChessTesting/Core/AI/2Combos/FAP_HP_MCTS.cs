using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessTesting.Core.AI.TwoCombos
{
    // A combination of FAP MCTS and MCTS-HP.
    class FAP_HP_MCTS : BasicMCTS
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

        public FAP_HP_MCTS(int maxNumOfPlayouts, int playoutDepthLimit, string name, string bmrkID, int numOfSegments, bool exponentialSegmentation, bool exponentialMultiplication) : base(maxNumOfPlayouts, playoutDepthLimit, name, bmrkID)
        {
            this.numOfSegments = numOfSegments;
            this.exponentialSegmentation = exponentialSegmentation;
            this.exponentialMultiplication = exponentialMultiplication;
        }

        protected override float EvaluateGame(Board b, bool team)
        {
            var moves = moveGenerator.GenerateMoves(b);

            float baseReward = evaluation.EvalBoard(b, !team, moves.Count == 0 && moveGenerator.InCheck());
             
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

            return baseReward * multiplicativeFactor;
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
                int colorIndex = node.State.WhiteToMove ? 0 : 1;
                evaluation.board = node.State;

                if (value > 0)
                {
                    currentNode.Value += value / (evaluation.CountMaterial(1 - colorIndex) + Evaluation.kingValue);
                }
                else
                {
                    currentNode.Value += value / (evaluation.CountMaterial(colorIndex) + Evaluation.kingValue);
                }

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

            return $"FAP HP MCTS, segments: {numOfSegments}, segmentation: {segmentation}, multiplication: {multiplication}, playouts: {maxNumOfPlayouts}";
        }
    }
}
