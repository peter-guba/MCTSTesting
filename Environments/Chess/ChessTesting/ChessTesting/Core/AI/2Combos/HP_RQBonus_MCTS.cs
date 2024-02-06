using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessTesting.Core.AI.TwoCombos
{
    // A combination of MCTS HP and Relative Bonus MCTS.
    class HP_RQBonus_MCTS : BasicMCTS
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

        private int numOfSteps;

        public HP_RQBonus_MCTS(int maxNumOfPlayouts, int playoutDepthLimit, string name, string bmrkID, float k, bool rBE, bool qBE) : base(maxNumOfPlayouts, playoutDepthLimit, name, bmrkID)
        {
            _k = k;

            if (rBE != true || qBE == true)
            {
                throw new ArgumentException("Only relative bonus can be enabled togerther with MCTS HP.");
            }
        }

        protected override void ResetVariables()
        {
            maxDepth = 0;
            playoutCounter = 0;
            bestMoveCounter = 0;
            _averageWinDistance = 0;
            _sampleStandardRelativeDeviation = 0;
            _averageDistance = 0;
            _traversalDistance = 0;
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

        protected override void Backpropagate(MCTSNode node, float val)
        {
            int colorIndex = node.State.WhiteToMove ? 0 : 1;
            evaluation.board = node.State;

            if (val > 0)
            {
                node.Value += val / (evaluation.CountMaterial(1 - colorIndex) + Evaluation.kingValue);
            }
            else
            {
                node.Value += val / (evaluation.CountMaterial(colorIndex) + Evaluation.kingValue);
            }

            ++node.Visits;

            if (node.parent != null)
            {
                Backpropagate(node.parent, -val);
            }
        }

        protected override float EvaluateGame(Board b, bool team)
        {            
            var moves = moveGenerator.GenerateMoves(b);
            float reward = evaluation.EvalBoard(b, !team, moves.Count == 0 && moveGenerator.InCheck());

            float resultSign = Math.Sign(reward);

            int distance = _traversalDistance + numOfSteps;

            if (_sampleStandardRelativeDeviation > 0)
            {
                // Compute the relative bonus based on simulation length.
                float lambdaR = Bonus(_averageDistance - (distance), _sampleStandardRelativeDeviation);
                reward += Math.Sign(reward) * GetAlpha(_averageDistance, _averageWinDistance, winDTimesDistanceSum, winDSum, distanceSum, _sampleStandardRelativeDeviation) * lambdaR;
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

            return reward;
        }

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

        /// <summary>
        /// Computes the bonus from the sample standard deviation and offset from the mean.
        /// </summary>
        private float Bonus(double offsetFromMean, double sampleStandardDeviation)
        {
            double lambda = offsetFromMean / sampleStandardDeviation;
            return -1 + 2 / (1 + (float)Math.Exp(-_k * lambda));
        }

        /// <summary>
        /// Computes the alpha multiplier used when computing the bonus.
        /// </summary>
        private float GetAlpha(float averageMetric, float averageWinMetric, float winMTimesMetricSum, float winMSum, float metricSum, float sampleStandardDeviation)
        {
            float covariance = winMTimesMetricSum - winMSum * averageMetric - metricSum * averageWinMetric + playoutCounter * averageWinMetric * averageMetric;
            covariance /= playoutCounter - 1;

            return Math.Abs(covariance / sampleStandardDeviation);
        }

        public override string ToString()
        {
            return $"MCTS RBonus HP, playouts: {maxNumOfPlayouts}";
        }
    }
}
