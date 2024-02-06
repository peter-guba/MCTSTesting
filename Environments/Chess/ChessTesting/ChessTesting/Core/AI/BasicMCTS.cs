using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;

namespace ChessTesting
{
    /// <summary>
    /// The vanilla version of MCTS.
    /// </summary>
    public class BasicMCTS : ISearch
    {
        protected MoveGenerator moveGenerator;

        /// <summary>
        /// The best move that the algorithm has found thus far.
        /// It is updated continuously.
        /// </summary>
        protected Move bestMove;
    
        /// <summary>
        /// The score that was given to the best move.
        /// </summary>
        protected int bestEval;

        /// <summary>
        /// The current game state.
        /// </summary>
        protected Board board;

        /// <summary>
        /// An object that provides functions used to evaluate a game.
        /// </summary>
        protected Evaluation evaluation;

        /// <summary>
        /// The C parameter used in UCB.
        /// </summary>
        protected readonly float C = 1.0f;

        protected System.Random rand;

        /// <summary>
        /// The number of playouts that the algorithm performs before picking a move.
        /// </summary>
        protected int maxNumOfPlayouts;

        /// <summary>
        /// The maximum depth that a playout can reach.
        /// </summary>
        protected int playoutDepthLimit;

        /// <summary>
        /// The name of the battle currently in progress (used when printing data).
        /// </summary>
        protected string battleName;

        /// <summary>
        /// A string that gets added to the ends of files that contain time and depth logs
        /// of battles in order to create separate files for different battles. This is necessary
        /// in order to compute the confidence bounds of round counts.
        /// </summary>
        protected string randomBattleString;

        /// <summary>
        /// The number of playouts performed thus far.
        /// </summary>
        protected int playoutCounter;

        /// <summary>
        /// The name of the algorithm.
        /// </summary>
        public readonly string name;

        /// <summary>
        /// An id of the benchmark that is currently running.
        /// </summary>
        protected string bmrkID;

        /// <summary>
        /// Path to the folder where secondary metrics are stored.
        /// </summary>
        public const string secondaryMetricsPath = "./time_depth_data/";

        /// <summary>
        /// The maximum depth that a node in the tree has reached thus far.
        /// </summary>
        protected int maxDepth = 0;

        /// <summary>
        /// Used in checkmate tests. Counts the number of times that the algorithm has
        /// identified the correct move without error.
        /// </summary>
        protected int bestMoveCounter = 0;

        /// <summary>
        /// Used in checkmate tests. The number of times in a row that the algorithm must
        /// identify the correct move to make in order to succeed.
        /// </summary>
        protected int bestMoveCounterLimit = 100;

        /// <summary>
        /// Used in checkmate tests. Lists the possible correct moves for the algorithm to identify.
        /// </summary>
        protected List<Move> bestMoveOptions;

        /// <summary>
        /// Determines whether the algorithm is supposed to stop when it reaches the maximum
        /// number of playouts or keep running until one of the moves in bestMoveOptions is
        /// correctly identified 100 times in one streak (the latter option is reffered to
        /// as the checkmate test).
        /// true <=> keep looking for best move
        /// </summary>
        public bool lookForBestMove = false;

        public BasicMCTS(int maxNumOfPlayouts, int playoutDepthLimit, string name, string bmrkID)
        {
            evaluation = new Evaluation();
            moveGenerator = new MoveGenerator();
            rand = new System.Random();

            this.maxNumOfPlayouts = maxNumOfPlayouts;
            this.playoutDepthLimit = playoutDepthLimit;
            this.name = name;
            this.bmrkID = bmrkID;
        }

        public void SetBoard(Board b)
        {
            board = b;
        }

        /// <summary>
        /// Searches for a move and returns it.
        /// </summary>
        public Move StartSearch()
        {
            if (!Directory.Exists(secondaryMetricsPath + name))
            {
                Directory.CreateDirectory(secondaryMetricsPath + name);
            }
            ResetVariables();
            return StartSearchBasic();
        }

        /// <summary>
        /// Resets the variables that need to be reset before the algorithm can
        /// be run again.
        /// </summary>
        protected virtual void ResetVariables()
        {
            maxDepth = 0;
            playoutCounter = 0;
            bestMoveCounter = 0;
        }

        private Move StartSearchBasic()
        {
            // Initialize search settings
            bestEval = 0;
            bestMove = Move.InvalidMove;
            moveGenerator.promotionsToGenerate = MoveGenerator.PromotionMode.All;

            //Console.WriteLine("y tho?");
            SearchMoves();

            return bestMove;
        }

        /// <summary>
        /// Uses MCTS to search for a good move to play.
        /// </summary>
        protected virtual void SearchMoves()
        {
            MCTSNode root = new MCTSNode(board, board.WhiteToMove, new Move(), moveGenerator, evaluation, null, 0);
            
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
                        (!BestMoveFound(root) || bestMoveCounter < bestMoveCounterLimit)
                    )
                    )
                {
                    if (lookForBestMove && bestMoveCounter > 0 && bestMoveCounter % 10 == 0)
                    {
                        GameManager.LogCheckmateTests(this);
                    }

                    // A timestamp used to measure how long it takes for the algorithm to perform a single iteration.
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

        /// <summary>
        /// Performs the first two steps of the MCTS algorithm - selection and expansion.
        /// </summary>
        protected virtual MCTSNode SelectAndExpand(MCTSNode start)
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
                MCTSNode bestChild = SelectBestChild(start, UCB);

                return SelectAndExpand(bestChild);
            }
            // Otherwise return this node.
            else
            {
                return start.Expand();
            }
        }

        /// <summary>
        /// Selects the best child of a given node using a given policy.
        /// </summary>
        protected virtual MCTSNode SelectBestChild(MCTSNode parent, Func<MCTSNode, bool, float> treePolicy)
        {
            MCTSNode bestChild = null;
            float bestScore = 0.0f;

            foreach (MCTSNode child in parent.Children)
            {
                float score = treePolicy(child, true);
                if (bestChild == null || bestScore < score)
                {
                    bestChild = child;
                    bestScore = score;
                }
            }

            return bestChild;
        }

        /// <summary>
        /// Computes the UCB score of a given node.
        /// </summary>
        /// <param name="scaleToZeroOne"> Determines whether the mean value of the node is first
        /// supposed to be scaled to the range [0, 1].</param>
        protected float UCB(MCTSNode n, bool scaleToZeroOne)
        {
            if (scaleToZeroOne) {
                // Scale the exploitation value between 0 and 1.
                float exploitation = (1.0f + n.Value / n.Visits) / 2;
                return exploitation + C * (float)Math.Sqrt(Math.Log(n.parent.Visits) / n.Visits);
            }
            else
            {
                float exploit = n.Value / n.Visits;
                float explore = C * (float)Math.Sqrt(Math.Log(n.parent.Visits) / n.Visits);
                return exploit + explore;
            }
        }

        /// <summary>
        /// Evaluates the given chess board with respect to the given team.
        /// </summary>
        protected virtual float EvaluateGame(Board b, bool team)
        {
            var moves = moveGenerator.GenerateMoves(b);

            // If there are still moves to be made or the other player isn't in check, then the game
            // hasn't finished, so return 0.
            if (moves.Count != 0 || !moveGenerator.InCheck())
            {
                return 0;
            }

            // If the given team lost (because they can't make any more moves), return 1.
            // This seems wrong at first glance, as the loser should be assigned -1. However,
            // in this implementation, the score stored in a given node is supposed to be
            // considered from the point of view of the parent, so this is the same thing
            // as inverting the team parameter.
            if (b.WhiteToMove == team)
            {
                return 1;
            }
            else
            {
                return -1;
            }
        }

        /// <summary>
        /// Performs a playout from the given node.
        /// </summary>
        public virtual Board Simulate(MCTSNode node)
        {
            playoutCounter++;

            if (node.isTerminal)
            {
                return node.State.Clone();
            }

            Board currentState = node.State.Clone();
            int numOfSteps = 0;
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
        /// Backpropagates the result of a playout up the search tree.
        /// </summary>
        protected virtual void Backpropagate(MCTSNode node, float val)
        {
            node.Value += val;

            ++node.Visits;

            if (node.parent != null)
            {
                Backpropagate(node.parent, -val);
            }
        }

        public override string ToString()
        {
            return $"Basic MCTS, playouts: {maxNumOfPlayouts}";
        }

        /// <summary>
        /// Logs the secondary metrics (the time it took to for the algorithm to run a single
        /// iteration and the maximum depth that it has reached thus far) using the given stream writer.
        /// </summary>
        protected void LogTimeAndDepth(double time, StreamWriter sw)
        {
            sw.WriteLine(playoutCounter + ", " + maxDepth + ", " + time);
        }

        public void SetBattleName(string name)
        {
            battleName = name;
        }

        public void SetRndBattleString(string str)
        {
            randomBattleString = str;
        }

        /// <summary>
        /// Used in checkmate tests. Sets the possible moves that the algorithm
        /// is supposed to identify.
        /// </summary>
        /// <param name="options"></param>
        public void SetBestMoveOptions(List<Move> options)
        {
            bestMoveOptions = options;
        }

        /// <summary>
        /// Used in checkmate tests. Determines whether one of the best moves
        /// was correctly identified by the algorithm.
        /// </summary>
        /// <param name="root"> The root node of the search tree. </param>
        protected bool BestMoveFound(MCTSNode root)
        {
            if (root.Children.Count == 0)
            {
                return false;
            }

            MCTSNode bestChild = null;
            float bestScore = 0.0f;

            foreach (MCTSNode child in root.Children)
            {
                float score = child.Value / child.Visits;
                if (bestChild == null || score > bestScore)
                {
                    bestChild = child;
                    bestScore = score;
                }
            }

            if (bestMoveOptions.Contains(bestChild.Action))
            {
                ++bestMoveCounter;
                return true;
            }
            else
            {
                bestMoveCounter = 0;
                return false;
            }
        }

        /// <summary>
        /// Returns the number of playouts that the algorithm has performed thus far.
        /// </summary>
        public int GetPlayoutCounter()
        {
            return playoutCounter;
        }

        /// <summary>
        /// Used in checkmate tests. Returns the number of times in a row that the algorithm
        /// has correctly identified one of the best moves to make.
        /// </summary>
        public int GetBestMoveCounter()
        {
            return bestMoveCounter;
        }
    }
}