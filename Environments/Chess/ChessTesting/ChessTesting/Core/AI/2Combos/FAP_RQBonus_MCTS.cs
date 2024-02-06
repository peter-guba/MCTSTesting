using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessTesting.Core.AI.TwoCombos
{
    // A combination of FAP MCTS and Relative/Quaitative Bonus MCTS.
    class FAP_RQBonus_MCTS : BasicMCTS
    {
        /// <summary>
        /// A constant used in computing the sigmoid function of the bonuses.
        /// </summary>
        private readonly float _k = 1.0f;

        /// <summary>
        /// The simulation distance from the root to the node created in the expansion phase.
        /// </summary>
        private int _traversalDistance = 0;

        /// <summary>
        /// The average combined distance of the partial game tree traversal and the playout during
        /// an iteration.
        /// </summary>
        private float _averageDistance = 0;

        /// <summary>
        /// The sample standard deviation of the sampled simulation lengths.
        /// </summary>
        private float _sampleStandardRelativeDeviation = 0;

        /// <summary>
        /// Like averageDistance, but all the lengths of playouts where player 0 lost get substituted with zeros.
        /// </summary>
        private float _averageWinDistance = 0;

        /// <summary>
        /// Sum of measured playout lengths.
        /// </summary>
        private int distanceSum = 0;

        /// <summary>
        /// Sum of squares of measured playout lengths.
        /// </summary>
        private int squareDistanceSum = 0;

        /// <summary>
        /// The sum of measured playout lengths in playouts that led to a win.
        /// </summary>
        private int winDSum = 0;

        /// <summary>
        /// Sum of multiplications of playout lengths with playout lengths of playouts that
        /// led to wins.
        /// </summary>
        private int winDTimesDistanceSum = 0;

        /// <summary>
        /// The average quality of final states encountered in playouts.
        /// </summary>
        private float _averageQuality = 0;

        /// <summary>
        /// The sample standard deviation of the sampled simulation lengths.
        /// </summary>
        private float _sampleStandardQualitativeDeviation = 0;

        /// <summary>
        /// Like averageQuality, but all the lengths of playouts where player 0 lost get substituted with zeros.
        /// </summary>
        private float _averageWinQuality = 0;

        /// <summary>
        /// Determines whether the relative bonus is supposed to be added to the result.
        /// </summary>
        private bool relativeBonusEnabled;

        /// <summary>
        /// Determines whether the qualitative bonus is supposed to be added to the result.
        /// </summary>
        private bool qualitativeBonusEnabled;

        /// <summary>
        /// Sum of measured final state qualities.
        /// </summary>
        private float qualitySum = 0.0f;

        /// <summary>
        /// Sum of squares of measured final state qualities.
        /// </summary>
        private float squareQualitySum = 0.0f;

        /// <summary>
        /// Sum of measured final state qualities from playouts that led to wins.
        /// </summary>
        private float winQSum = 0.0f;

        /// <summary>
        /// Sum of multiplications of final state qualities with final state qualities from playouts that
        /// led to wins.
        /// </summary>
        private float winQTimesQualitySum = 0.0f;

        private int numOfSteps;
        
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

        public FAP_RQBonus_MCTS(int maxNumOfPlayouts, int playoutDepthLimit, string name, string bmrkID, int numOfSegments, bool exponentialSegmentation, bool exponentialMultiplication, float k, bool rBE, bool qBE) : base(maxNumOfPlayouts, playoutDepthLimit, name, bmrkID)
        {
            this.numOfSegments = numOfSegments;
            this.exponentialSegmentation = exponentialSegmentation;
            this.exponentialMultiplication = exponentialMultiplication;

            _k = k;
            relativeBonusEnabled = rBE;
            qualitativeBonusEnabled = qBE;
        }

        protected override void ResetVariables()
        {
            maxDepth = 0;
            playoutCounter = 0;
            bestMoveCounter = 0;
            _averageWinQuality = 0;
            _sampleStandardQualitativeDeviation = 0;
            _averageQuality = 0;
            _averageWinDistance = 0;
            _sampleStandardRelativeDeviation = 0;
            _averageDistance = 0;
            _traversalDistance = 0;
            winQTimesQualitySum = 0.0f;
            winQSum = 0.0f;
            qualitySum = 0.0f;
            squareQualitySum = 0.0f;
            winDTimesDistanceSum = 0;
            winDSum = 0;
            distanceSum = 0;
            squareDistanceSum = 0;
        }

        // Overridden so that the depth of the tree can be measured using the _traversalDistance variable.
        protected override MCTSNode SelectAndExpand(MCTSNode start)
        {
            _traversalDistance = 0;
            MCTSNode currentNode = start;

            while (true)
            {
                // If the current node has already been fully expanded, pick one of its
                // children using the UCB policy.
                if (currentNode.FullyExpanded)
                {
                    // If the node has no children it means its terminal, so it is returned.
                    if (currentNode.isTerminal)
                    {
                        return currentNode;
                    }

                    // Pick the best child according to UCB.
                    currentNode = SelectBestChild(currentNode, UCB);
                }
                // Otherwise return this node.
                else
                {
                    return currentNode.Expand();
                }

                _traversalDistance++;
            }
        }

        protected override float EvaluateGame(Board b, bool team)
        {
            float reward;
            var moves = moveGenerator.GenerateMoves(b);

            if (moves.Count != 0)
            {
                reward = 0;
            }
            else if (b.WhiteToMove == team)
            {
                reward = 1.0f;
            }
            else
            {
                // We lost or are in bad position
                reward = -1.0f;
            }

            float resultSign = reward;

            // Compute relative bonus, if it is enabled.
            if (relativeBonusEnabled)
            {
                int distance = _traversalDistance + numOfSteps;

                if (_sampleStandardRelativeDeviation > 0)
                {
                    // Compute the relative bonus based on simulation length.
                    float lambdaR = Bonus(_averageDistance - (distance), _sampleStandardRelativeDeviation);
                    reward += reward * GetAlpha(_averageDistance, _averageWinDistance, winDTimesDistanceSum, winDSum, distanceSum, _sampleStandardRelativeDeviation) * lambdaR;
                }

                // Update the relevant variables.
                _averageDistance = (_averageDistance * (playoutCounter - 1) + distance) / playoutCounter;
                _averageWinDistance = (_averageWinDistance * (playoutCounter - 1) + Math.Max(0, resultSign) * distance) / playoutCounter;
                distanceSum += distance;
                winDSum += (int)Math.Max(0, resultSign) * distance;
                winDTimesDistanceSum += distance * (int)Math.Max(0, resultSign) * distance;
                squareDistanceSum += distance * distance;

                if (playoutCounter > 1)
                {
                    _sampleStandardRelativeDeviation = squareDistanceSum - 2 * distanceSum * _averageDistance + playoutCounter * _averageDistance * _averageDistance;
                    _sampleStandardRelativeDeviation = (float)Math.Sqrt(_sampleStandardRelativeDeviation / (playoutCounter - 1));
                }
            }

            // Compute qualitative bonus, if it is enabled.
            if (qualitativeBonusEnabled)
            {
                float quality = evaluation.EvalBoard(b, !team, moves.Count == 0 && moveGenerator.InCheck()) / Evaluation.maxBoardValue;

                if (_sampleStandardQualitativeDeviation > 0)
                {
                    // Compute the qualitative bonus based on outcome quality.
                    float lambdaR = Bonus(quality - _averageQuality, _sampleStandardQualitativeDeviation);
                    reward += reward * GetAlpha(_averageQuality, _averageWinQuality, winQTimesQualitySum, winQSum, qualitySum, _sampleStandardQualitativeDeviation) * lambdaR;
                }

                // Update the relevant variables.
                _averageQuality = (_averageQuality * (playoutCounter - 1) + quality) / playoutCounter;
                _averageWinQuality = (_averageQuality * (playoutCounter - 1) + Math.Max(0, resultSign) * quality) / playoutCounter;
                qualitySum += quality;
                winQSum += (float)Math.Max(0, resultSign) * quality;
                winQTimesQualitySum += quality * (float)Math.Max(0, resultSign) * quality;
                squareQualitySum += quality * quality;

                if (playoutCounter > 1)
                {
                    _sampleStandardQualitativeDeviation = squareQualitySum - 2 * qualitySum * _averageQuality + playoutCounter * _averageQuality * _averageQuality;
                    _sampleStandardQualitativeDeviation = (float)Math.Sqrt(_sampleStandardQualitativeDeviation / (playoutCounter - 1));
                }
            }

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

            return reward * multiplicativeFactor;
        }

        // Overridden so that the number of steps is stored in a class variable instead of just a local variable.
        public override Board Simulate(MCTSNode node)
        {
            playoutCounter++;
            numOfSteps = 0;

            if (node.isTerminal)
            {
                return node.State.Clone();
            }

            Board currentState = node.State.Clone();
            List<Move> moves = moveGenerator.GenerateMoves(currentState);

            // Keeps simulating while the game isn't finished, the maximum depth isn't reached
            // and there are more pieces than just the two kings (as it is impossible to
            // reach a checkmate then.
            while (moves.Count != 0 && numOfSteps < playoutDepthLimit && currentState.MoreThanKingsPresent())
            {
                currentState.MakeMove(moves[rand.Next(moves.Count)]);
                ++numOfSteps;
                moves = moveGenerator.GenerateMoves(currentState);
            }

            return currentState;
        }

        private float Bonus(double offsetFromMean, double sampleStandardDeviation)
        {
            double lambda = offsetFromMean / sampleStandardDeviation;
            return -1 + 2 / (1 + (float)Math.Exp(-_k * lambda));
        }

        private float GetAlpha(float averageMetric, float averageWinMetric, float winMTimesMetricSum, float winMSum, float metricSum, float sampleStandardDeviation)
        {
            float covariance = winMTimesMetricSum - winMSum * averageMetric - metricSum * averageWinMetric + playoutCounter * averageWinMetric * averageMetric;
            covariance /= playoutCounter - 1;

            return Math.Abs(covariance / sampleStandardDeviation);
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

            return $"FAP RQBonus MCTS, k: {_k}, relative bonus: {relativeBonusEnabled}, qualitative bonus: {qualitativeBonusEnabled}, segments: {numOfSegments}, segmentation: {segmentation}, multiplication: {multiplication}, playouts: {maxNumOfPlayouts}";
        }
    }
}
