using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessTesting.Core.AI.TwoCombos
{
    // A combination of VOI-aware MCTS and WP MCTS.
    class VOI_WP_MCTS : BasicMCTS
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

        public VOI_WP_MCTS(int maxNumOfPlayouts, int playoutDepthLimit, string name, string bmrkID, float vB, float pB, bool relative) : base(maxNumOfPlayouts, playoutDepthLimit, name, bmrkID)
        {
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

        protected override MCTSNode SelectAndExpand(MCTSNode node)
        {
            while (true)
            {
                // If node is not fully expanded - generate new child node with this new action and return it.
                if (!node.FullyExpanded)
                {
                    return node.Expand();
                }
                else if (node.isTerminal)
                {
                    return node;
                }

                // Find child node with highest value and call Selection recursively.
                MCTSNode bestChild = null;

                if (node.FullyExpanded && node.Children.Count == 1)
                {
                    bestChild = node.Children[0];
                }
                else if (node.parent == null)
                {
                    MCTSNode secondBestChild = null;
                    double secondBestScore = 0.0;
                    double bestScore = 0.0;
                    foreach (MCTSNode child in node.Children)
                    {
                        double childScore = (1.0 + child.Value / child.Visits) / 2;
                        if (childScore > bestScore || bestChild == null)
                        {
                            secondBestScore = bestScore;
                            bestScore = childScore;
                            secondBestChild = bestChild;
                            bestChild = child;
                        }
                        else if (childScore > secondBestScore || secondBestChild == null)
                        {
                            secondBestChild = child;
                            secondBestScore = childScore;
                        }
                    }

                    MCTSNode childWithBestVOI = null;
                    bestScore = 0.0;
                    foreach (MCTSNode child in node.Children)
                    {
                        double childScore;
                        if (child == bestChild)
                        {
                            childScore = GetVOIBest(bestChild, secondBestChild);
                        }
                        else
                        {
                            childScore = GetVOIOther(bestChild, child);
                        }

                        if (childScore > bestScore || childWithBestVOI == null)
                        {
                            bestScore = childScore;
                            childWithBestVOI = child;
                        }
                    }

                    bestChild = childWithBestVOI;
                }
                else
                {
                    bestChild = SelectBestChild(node, UCB);
                }

                if (bestChild == null)
                    return null;

                node = bestChild;
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

            float result = 0;
            float sumOfWeights = 0;

            for (int i = 0; i < intermediateResults.Count; ++i)
            {
                float weight = (probabilityOfEncounter(i, intermediateResults.Count) + valueOfInformation(i, intermediateResults.Count)) / 2;
                sumOfWeights += weight;
                result += intermediateResults[i] * weight;
            }

            result /= (sumOfWeights * upperBound);

            if (float.IsNaN(result) || float.IsInfinity(result) || float.IsNegativeInfinity(result))
            {
                Console.WriteLine(sumOfWeights);
                Console.WriteLine(upperBound);
                Console.WriteLine(intermediateResults.Count);
                throw new ArithmeticException("Bad Juju");
            }

            return result;
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

        /// <summary>
        /// Computes an estimate of the VOI obtained by sampling the current best child node.
        /// </summary>
        private double GetVOIBest(MCTSNode bestChild, MCTSNode secondBestChild)
        {
            double bestChildAvgVal = (1.0 + bestChild.Value / bestChild.Visits) / 2;
            double secondBestChildAvgVal = (1.0 + secondBestChild.Value / secondBestChild.Visits) / 2;
            return secondBestChildAvgVal / (bestChild.Visits + 1) *
                Math.Exp(-2.0 * Math.Pow(bestChildAvgVal - secondBestChildAvgVal, 2.0) * bestChild.Visits);
        }

        /// <summary>
        /// Computes an estimate of the VOI obtained by a child node that isn't currently the best.
        /// </summary>
        private double GetVOIOther(MCTSNode bestChild, MCTSNode otherChild)
        {
            double bestChildAvgVal = (1.0 + bestChild.Value / bestChild.Visits) / 2;
            double otherChildAvgVal = (1.0 + otherChild.Value / otherChild.Visits) / 2;
            return (1 - bestChildAvgVal) / (otherChild.Visits + 1) *
                Math.Exp(-2.0 * Math.Pow(bestChildAvgVal - otherChildAvgVal, 2.0) * otherChild.Visits);
        }

        public override string ToString()
        {
            return $"VOI + WP MCTS, voiBase: {voiBase}, poeBase: {poeBase}, normalized: {relative}, playouts: {maxNumOfPlayouts}";
        }
    }
}
