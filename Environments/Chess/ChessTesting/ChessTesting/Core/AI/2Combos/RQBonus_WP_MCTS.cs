using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessTesting.Core.AI.TwoCombos
{
    // A combination of Relative Bonus MCTS and WP MCTS.
    internal class RQBonus_WP_MCTS : BasicMCTS
    {
        /// <summary>
        /// The base used when computing an estimate of the value of information provided
        /// by a state.
        /// </summary>
        private float voiBase;

        /// <summary>
        /// The base used when computing an estimate of the probability of encountering
        /// a state.
        /// </summary>
        private float poeBase;

        /// <summary>
        /// Determines whether the score of the state from which a playout starts is
        /// supposed to be subtracted from the scores of the states encountered during
        /// a playout.
        /// </summary>
        private bool relative;

        /// <summary>
        /// A list of scores of all the states encountered during the last playout.
        /// </summary>
        private List<float> intermediateResults;

        /// <summary>
        /// An upper bound on the value that a chess board can have during a playout.
        /// It is computed at the beginning of every run of the algorithm.
        /// </summary>
        protected float upperBound;

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

        public RQBonus_WP_MCTS(int maxNumOfPlayouts, int playoutDepthLimit, string name, string bmrkID, float k, bool rBE, bool qBE, float vB, float pB, bool relative) : base(maxNumOfPlayouts, playoutDepthLimit, name, bmrkID)
        {
            _k = k;

            voiBase = vB;
            poeBase = pB;
            this.relative = relative;

            if (rBE != true || qBE == true)
            {
                throw new ArgumentException("Only relative bonus can be enabled togerther with WP MCTS.");
            }
        }

        protected override void SearchMoves()
        {
            MCTSNode root = new MCTSNode(board, board.WhiteToMove, new Move(), moveGenerator, evaluation, null, 0);

            // This evaluation is here only so that the board is set in the evaluation object when the
            // upper bound is computed.
            evaluation.EvalBoard(root.State, true, true);



            // IF YOU ARE STARTING FROM A BOARD WITH ONLY KINGS, THIS WILL BE ZERO.



            upperBound = Math.Max(evaluation.CountMaterial(0), evaluation.CountMaterial(1));

            using (var sw = new StreamWriter(secondaryMetricsPath + name + "/" + bmrkID + "_" + battleName + "_" + randomBattleString + ".txt", true))
            {
                // If we aren't looking for the best move, just perform the maximum allowed number of iterations.
                // If we are looking for the best move, then keep iterating until either the maximum allowed number of iterations
                // has been exceeded, or the best move has been found the required number of times after trying every
                // action at least once.
                while (
                    (!lookForBestMove && maxNumOfPlayouts > playoutCounter) ||
                    (
                        lookForBestMove &&
                        playoutCounter < maxNumOfPlayouts &&
                        (playoutCounter <= root.Children.Count || !BestMoveFound(root) || bestMoveCounter < bestMoveCounterLimit)
                    )
                    )
                {
                    if (lookForBestMove && bestMoveCounter > 0 && bestMoveCounter % 10 == 0)
                    {
                        GameManager.LogCheckmateTests(this);
                    }

                    long timeStamp = Stopwatch.GetTimestamp();

                    MCTSNode best = SelectAndExpand(root);

                    if (best.depth > maxDepth)
                    {
                        maxDepth = best.depth;
                    }

                    Board result = Simulate(best);
                    float score = EvaluateGame(result, best.team);
                    Backpropagate(best, score);

                    bestMove = root.GetBestMove();

                    if (!lookForBestMove)
                    {
                        LogTimeAndDepth(1_000_000_000.0 * (Stopwatch.GetTimestamp() - timeStamp) / Stopwatch.Frequency, sw);
                    }
                }

                // If we are running checkmate tests, output some data.
                if (lookForBestMove)
                {
                    Console.WriteLine($"number of root actions: {root.Children.Count}");
                    Console.WriteLine($"number of playouts: {playoutCounter - bestMoveCounter}");
                    Console.WriteLine($"best move: {bestMove.Name}");
                    Console.WriteLine();
                }
            }
        }

        protected override MCTSNode SelectBestChild(MCTSNode parent, Func<MCTSNode, bool, float> treePolicy)
        {
            MCTSNode bestChild = null;
            float bestScore = 0.0f;

            foreach (MCTSNode child in parent.Children)
            {
                float score = treePolicy(child, false);
                if (bestChild == null || bestScore < score)
                {
                    bestChild = child;
                    bestScore = score;
                }
            }

            return bestChild;
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
            float reward = 0;
            if (upperBound != 0)
            {
                float sumOfWeights = 0;

                for (int i = 0; i < intermediateResults.Count; ++i)
                {
                    float weight = (probabilityOfEncounter(i, intermediateResults.Count) + valueOfInformation(i, intermediateResults.Count)) / 2;
                    sumOfWeights += weight;
                    reward += intermediateResults[i] * weight;
                }

                reward /= sumOfWeights * upperBound;

                if (float.IsNaN(reward) || float.IsInfinity(reward) || float.IsNegativeInfinity(reward))
                {
                    Console.WriteLine(sumOfWeights);
                    Console.WriteLine(upperBound);
                    Console.WriteLine(intermediateResults.Count);
                    throw new ArithmeticException("Bad Juju");
                }
            }

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

            intermediateResults = new List<float>();
            Board currentState = node.State.Clone();
            List<Move> moves = moveGenerator.GenerateMoves(currentState);
            float initialEval = evaluation.EvalBoard(currentState, !node.team, moves.Count == 0 && moveGenerator.InCheck());

            if (node.isTerminal || !currentState.MoreThanKingsPresent())
            {
                intermediateResults.Add(initialEval);
                return node.State.Clone();
            }

            // Keeps simulating while the game isn't finished, the maximum depth isn't reached
            // and there are more pieces than just the two kings (as it is impossible to
            // reach a checkmate then.
            while (moves.Count != 0 && numOfSteps < playoutDepthLimit && currentState.MoreThanKingsPresent())
            {
                currentState.MakeMove(moves[rand.Next(moves.Count)]);

                if (numOfSteps % 2 == 0)
                {
                    if (relative)
                    {
                        intermediateResults.Add(evaluation.EvalBoard(currentState, !node.team, moves.Count == 0 && moveGenerator.InCheck()) - initialEval);
                    }
                    else
                    {
                        intermediateResults.Add(evaluation.EvalBoard(currentState, !node.team, moves.Count == 0 && moveGenerator.InCheck()));
                    }
                }
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

        /// <summary>
        /// Estimates the importance of a state based on the probability of
        /// it being encountered (the earlier it occurs, the higher).
        /// </summary>
        private float probabilityOfEncounter(int index, int length)
        {
            return (float)Math.Min(Math.Pow(poeBase, -index), 10000);
        }

        /// <summary>
        /// Estimates the importance of a state based on the value of the
        /// information contained in it (the later it occurs, the higher).
        /// </summary>
        private float valueOfInformation(int index, int length)
        {
            return (float)Math.Min(Math.Pow(voiBase, index - length), 10000);
        }

        public override string ToString()
        {
            return $"Relative Bonus + WP MCTS, voiBase: {voiBase}, poeBase: {poeBase}, normalized: {relative}, k: {_k}, playouts: {maxNumOfPlayouts}";
        }
    }
}
