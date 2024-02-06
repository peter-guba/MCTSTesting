using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessTesting.Core.AI.TwoCombos
{
    // A combination of FAP MCTS and WP MCTS.
    class FAP_WP_MCTS : BasicMCTS
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

        public FAP_WP_MCTS(int maxNumOfPlayouts, int playoutDepthLimit, string name, string bmrkID, int numOfSegments, bool exponentialSegmentation, bool exponentialMultiplication, float vB, float pB, bool relative) : base(maxNumOfPlayouts, playoutDepthLimit, name, bmrkID)
        {
            this.numOfSegments = numOfSegments;
            this.exponentialSegmentation = exponentialSegmentation;
            this.exponentialMultiplication = exponentialMultiplication;

            voiBase = vB;
            poeBase = pB;
            this.relative = relative;
        }

        protected override void SearchMoves()
        {
            MCTSNode root = new MCTSNode(board, board.WhiteToMove, new Move(), moveGenerator, evaluation, null, 0);

            // This evaluation is here only so that the board is set in the evaluation object when the
            // upper bound is computed.
            evaluation.EvalBoard(root.State, true, true);
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

        public override Board Simulate(MCTSNode node)
        {
            playoutCounter++;

            intermediateResults = new List<float>();
            Board currentState = node.State.Clone();
            int numOfSteps = 0;
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

        protected override float EvaluateGame(Board b, bool team)
        {
            if (upperBound == 0)
            {
                return 0;
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

            float result = 0;
            float sumOfWeights = 0;

            for (int i = 0; i < intermediateResults.Count; ++i)
            {
                float weight = (probabilityOfEncounter(i, intermediateResults.Count) + valueOfInformation(i, intermediateResults.Count)) / 2;
                sumOfWeights += weight;
                result += intermediateResults[i] * weight;
            }

            if (float.IsNaN(result) || float.IsInfinity(result) || float.IsNegativeInfinity(result))
            {
                Console.WriteLine(sumOfWeights);
                Console.WriteLine(upperBound);
                Console.WriteLine(intermediateResults.Count);
                throw new ArithmeticException("Bad Juju");
            }

            return multiplicativeFactor * result / (sumOfWeights * upperBound);
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
            string segmentation = exponentialSegmentation ? "exp" : "lin";
            string multiplication = exponentialMultiplication ? "exp" : "lin";

            return $"FAP WP MCTS, voiBase: {voiBase}, poeBase: {poeBase}, normalized: {relative}, segments: {numOfSegments}, segmentation: {segmentation}, multiplication: {multiplication}, playouts: {maxNumOfPlayouts}";
        }
    }
}
