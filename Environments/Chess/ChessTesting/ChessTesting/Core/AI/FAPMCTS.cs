using System;

namespace ChessTesting
{
    class FAPMCTS : BasicMCTS
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

        public FAPMCTS(int maxNumOfPlayouts, int playoutDepthLimit, string name, string bmrkID, int numOfSegments, bool exponentialSegmentation, bool exponentialMultiplication) : base(maxNumOfPlayouts, playoutDepthLimit, name, bmrkID)
        {
            this.numOfSegments = numOfSegments;
            this.exponentialSegmentation = exponentialSegmentation;
            this.exponentialMultiplication = exponentialMultiplication;
        }

        // Works just like in default MCTS, except it multiplies the returned values
        // by the computed multiplicative factor.
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

        // Works just like in default MCTS, it just has to increment the number of times
        // a node has been visited by the multiplicative factor used when evaluating
        // the previous playout.
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

            return $"FAP MCTS, segments: {numOfSegments}, segmentation: {segmentation}, multiplication: {multiplication}, playouts: {maxNumOfPlayouts}";
        }
    }
}
