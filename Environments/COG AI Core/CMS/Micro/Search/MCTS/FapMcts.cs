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
    /// A variant that applies a bonus to playouts based on the time at which they are performed (the later, the better).
    /// </summary>
    public class FapMcts : Mcts
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

        public FapMcts(
            IActionGenerator actionGenerator,
            int maxPlayouts,
            int numOfSegments,
            List<Player> players,
            bool exponentialSegmentation,
            bool exponentialMultiplication,
            string bmrkID,
            int playoutRoundLimit = int.MaxValue) : base(actionGenerator, maxPlayouts, players, bmrkID, playoutRoundLimit, "fap_mcts")
        {
            this.numOfSegments = numOfSegments;
            this.exponentialSegmentation = exponentialSegmentation;
            this.exponentialMultiplication = exponentialMultiplication;
        }

        // Works just like in default MCTS, it just has to increment the number of times
        // a node has been visited by the multiplicative factor used when evaluating
        // the previous playout.
        protected override void BackpropagateResults(MctsNode node, double value)
        {
            int segmentNumber = GetSegmentNumber(TotalIterCount);
            double multiplicativeFactor;

            if (exponentialMultiplication)
            {
                multiplicativeFactor = Math.Pow(2, segmentNumber - 1);
            }
            else
            {
                multiplicativeFactor = segmentNumber;
            }

            do
            {
                node.VisitedCount += (int)multiplicativeFactor;
                node.Value += value;
                node = node.Parent;
            } while (node != null);
        }

        // Works just like in default MCTS, except it multiplies the returned
        // values by the computed multiplicative factor.
        protected override double EvaluateGame(GameResult result, GameEnvironment environment)
        {
            int segmentNumber = GetSegmentNumber(TotalIterCount);
            double multiplicativeFactor;

            if (exponentialMultiplication)
            {
                multiplicativeFactor = Math.Pow(2, segmentNumber - 1);
            }
            else
            {
                multiplicativeFactor = segmentNumber;
            }

            if (result.Winner == GameState.WINNER_NONE)
            {
                return 0.0;
            }
            else
            {
                if (result.IsDraw)
                    return 0.0;
                else if (result.Winner == 0)
                    return multiplicativeFactor;
            }

            // We lost or are in bad position
            return -multiplicativeFactor;
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
                    double bound = _maxPlayouts * (Math.Pow(2, i) / aux);
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
                return ((n - 1) / (_maxPlayouts / numOfSegments)) + 1;
            }
        }

        public override string ToString()
        {
            string segmentation = exponentialSegmentation ? "exp" : "lin";
            string multiplication = exponentialMultiplication ? "exp" : "lin";

            return $"FAP MCTS, segments: {numOfSegments}, segmentation: {segmentation}, multiplication: {multiplication}, playouts: {_maxPlayouts}";
        }
    }
}