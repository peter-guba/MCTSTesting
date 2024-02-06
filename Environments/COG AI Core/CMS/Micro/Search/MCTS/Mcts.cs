using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using CMS.ActionGenerators;
using CMS.Actions;
using CMS.GameStateEval;
using CMS.Players;
using CMS.Playout;

namespace CMS.Micro.Search.MCTS
{
    public class Mcts
    {
        protected readonly IActionGenerator _actionGenerator;
        
        /// <summary>
        /// The number of playouts that the algorithm performs before picking a move.
        /// </summary>
        protected readonly int _maxPlayouts;

        /// <summary>
        /// The C parameter used in UCB.
        /// </summary>
        protected readonly static double C = 1.0;

        /// <summary>
        /// 
        /// </summary>
        protected readonly List<Player> _players;

        /// <summary>
        /// The maximum number of rounds that can be performed in a playout.
        /// </summary>
        protected readonly int _playoutRoundLimit;

        /// <summary>
        /// The number of iterations of the algorithm that has been performed thus far.
        /// Is reset every time the algorithm is started.
        /// </summary>
        public int TotalIterCount { get; protected set; }

        /// <summary>
        /// The path to the folder where secondary metrics are supposed to be outputted.
        /// </summary>
        public const string path = "./time_depth_data/";

        /// <summary>
        /// The name of the algorithm.
        /// </summary>
        public readonly string name;

        /// <summary>
        /// The maximum depth that a node in the tree has reached thus far.
        /// Gets reset every time the algorithm starts.
        /// </summary>
        protected int maxDepth;

        /// <summary>
        /// The id of the currently running benchmark.
        /// </summary>
        protected string bmrkID;

        /// <summary>
        /// The name of the battle that is currently being performed.
        /// </summary>
        protected string battleName;

        /// <summary>
        /// A string that gets added to the ends of files that contain time and depth logs
        /// of battles in order to create separate files for different battles. This is necessary
        /// in order to compute the confidence bounds of round counts.
        /// </summary>
        protected string randomBattleString;

        public Mcts(
            IActionGenerator actionGenerator,
            int maxPlayouts,
            List<Player> players,
            string bmrkID,
            int playoutRoundLimit = int.MaxValue,
            string name = "basic_mcts")
        {
            _actionGenerator = actionGenerator;
            _maxPlayouts = maxPlayouts;
            _players = players;
            _playoutRoundLimit = playoutRoundLimit;
            this.name = name;
            this.bmrkID = bmrkID;

            Directory.CreateDirectory(path + name);
        }

        /// <summary>
        /// Uses MCTS to search for good moves to play.
        /// </summary>
        internal virtual ICollection<ActionCms> GetActions(GameEnvironment environment)
        {
            TotalIterCount = 0;
            maxDepth = 0;
            var rootActionState = new ActionStatePair { Environment = environment };
            IEnumerable<ActionStatePair> rootChildActions = _actionGenerator.EnumerateActions(rootActionState.Environment);

            var root = new MctsNode(null, rootActionState, rootChildActions, 0);

            using (var sw = new StreamWriter(path + name + "/" + bmrkID + "_" + battleName + "_" + randomBattleString + ".txt", true))
            {
                while (TotalIterCount < _maxPlayouts)
                {
                    // A timestamp used to measure how long it takes for the algorithm to perform a single iteration.
                    long timeStamp = Stopwatch.GetTimestamp();

                    ++TotalIterCount;
                    // Selection and Expansion
                    MctsNode selectedNode = SelectAndExpand(root);

                    // Simulation
                    GameEnvironment selectedStateClone = selectedNode.CurrentActionState.Environment.DeepCloneState();
                    GameResult result = Game.Playout(selectedStateClone, _players, _playoutRoundLimit);
                    double value = EvaluateGame(result, selectedStateClone);

                    // Backpropagation
                    BackpropagateResults(selectedNode, value);

                    if (selectedNode.depth > maxDepth)
                    {
                        maxDepth = selectedNode.depth;
                    }

                    LogTimeAndDepth(1_000_000_000.0 * (Stopwatch.GetTimestamp() - timeStamp) / Stopwatch.Frequency, sw);
                }
            }

            MctsNode bestNode = SelectBestChild(root, environment.GameState.ActivePlayer, (n, p, b) => n.VisitedCount == 0 ? Double.NegativeInfinity : (p == 0 ? n.Value / n.VisitedCount : -n.Value / n.VisitedCount));
            var actions = bestNode.CurrentActionState.PlayerAction;

            return actions;
        }

        /// <summary>
        /// Performs the first two steps of the MCTS algorithm - selection and expansion.
        /// </summary>
        protected virtual MctsNode SelectAndExpand(MctsNode node)
        {
            Debug.Assert(node != null);
            while (true)
            {
                // If node is not fully expanded - generate new child node with this new action and return it
                MctsNode newChild;
                if (node.TryGetNextChild(_actionGenerator, out newChild))
                {
                    return newChild;
                }
                else if (node.IsTerminal)
                {
                    return node;
                }

                // Else find child node with highest value and call Selection recursively
                int player = node.CurrentActionState.Environment.GameState.ActivePlayer;
                MctsNode bestChild = SelectBestChild(node, player, Ucb);

                if (bestChild == null)
                    return null;

                node = bestChild;
            }
        }

        /// <summary>
        /// Selects the best child of a given node using a given policy.
        /// </summary>
        protected virtual MctsNode SelectBestChild(MctsNode node, int player, Func<MctsNode, int, bool, double> evalFunc)
        {
            double bestScore = 0.0;
            MctsNode bestChild = null;
            foreach (MctsNode child in node.Children)
            {
                double score = evalFunc(child, player, true);
                if (bestChild == null || score > bestScore)
                {
                    bestScore = score;
                    bestChild = child;
                }
            }

            return bestChild;
        }

        /// <summary>
        /// Backpropagates the result of a playout up the search tree.
        /// </summary>
        protected virtual void BackpropagateResults(MctsNode node, double value)
        {
            do
            {
                node.VisitedCount++;
                node.Value += value;
                node = node.Parent;
            } while (node != null);
        }

        /// <summary>
        /// Computes the UCB score of a given node.
        /// </summary>
        /// <param name="scaleToZeroOne"> Determines whether the mean value of the node is first
        /// supposed to be scaled to the range [0, 1].</param>
        internal static double Ucb(MctsNode node, int player, bool scaleToZeroOne)
        {
            if (scaleToZeroOne)
            {
                // Scale the exploitation value between 0 and 1.
                double exploit = player == 0 ? (1.0 + node.Value / node.VisitedCount) / 2 : (1.0 - node.Value / node.VisitedCount) / 2;
                double explore = C * Math.Sqrt(Math.Log(node.Parent.VisitedCount) / node.VisitedCount);
                return exploit + explore;
            }
            else
            {
                double exploit = node.Value / node.VisitedCount;
                double explore = C * Math.Sqrt(Math.Log(node.Parent.VisitedCount) / node.VisitedCount);
                return player == 0 ? (exploit + explore) : (exploit - explore);
            }
        }

        /// <summary>
        /// Computes a numeric evaluation of the result of the playout.
        /// </summary>
        protected virtual double EvaluateGame(GameResult result, GameEnvironment environment)
        {
            if (result.Winner == GameState.WINNER_NONE)
            {
                return 0.0;
            }
            else
            {
                if (result.IsDraw)
                    return 0.0;
                else if (result.Winner == 0)
                    return 1.0;
            }

            // We lost or are in bad position
            return -1.0;
        }

        /// <summary>
        /// Logs the secondary metrics (the time it took to for the algorithm to run a single
        /// iteration and the maximum depth that it has reached thus far) using the given stream writer.
        /// </summary>
        protected void LogTimeAndDepth(double time, StreamWriter sw)
        {
            sw.WriteLine(TotalIterCount + ", " + maxDepth + ", " + time);
        }

        public override string ToString()
        {
            return $"Basic MCTS, playouts: {_maxPlayouts}";
        }

        /// <summary>
        /// Sets the name of the battle that is currently running.
        /// </summary>
        public void SetBattleName(string name)
        {
            battleName = name;
        }

        /// <summary>
        /// Sets the random string that is added to the ends of files that contain time
        /// and depth logs.
        /// </summary>
        public void SetRndBattleString(string str)
        {
            randomBattleString = str;
        }
    }
}