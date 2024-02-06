using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using CMS.ActionGenerators;
using CMS.Actions;
using CMS.GameStateEval;
using CMS.Players;
using CMS.Playout;
using System.Linq;
using System.Text;
using System.Xml;
using QuickGraph;
using QuickGraph.Serialization;

namespace CMS.Micro.Search.MCTS
{
    /// <summary>
    /// A variant that computes a weighted sum of the scores of all the states encountered during a playout.
    /// </summary>
    public class WPMcts : Mcts
    {
        /// <summary>
        /// The base used when computing an estimate of the value of information provided
        /// by a state.
        /// </summary>
        private double voiBase;

        /// <summary>
        /// The base used when computing an estimate of the probability of encountering
        /// a state.
        /// </summary>
        private double poeBase;

        /// <summary>
        /// Determines whether the score of the state from which a playout starts is
        /// supposed to be subtracted from the scores of the states encountered during
        /// a playout.
        /// </summary>
        private bool relative;

        public WPMcts(
            IActionGenerator actionGenerator,
            int maxPlayouts,
            List<Player> players,
            double vB,
            double pB,
            bool normalize,
            string bmrkID,
            int playoutRoundLimit = int.MaxValue) : base(actionGenerator, maxPlayouts, players, bmrkID, playoutRoundLimit, "wp_mcts")
        {
            voiBase = vB;
            poeBase = pB;
            this.relative = normalize;
        }

        // Overridden so that intermediate states are collected during the playouts.
        internal override ICollection<ActionCms> GetActions(GameEnvironment environment)
        {
            TotalIterCount = 0;
            maxDepth = 0;
            var rootActionState = new ActionStatePair { Environment = environment };
            IEnumerable<ActionStatePair> rootChildActions = _actionGenerator.EnumerateActions(rootActionState.Environment);

            var root = new MctsNode(null, rootActionState, rootChildActions, 0);
            float upperBound = Math.Max(root.UnitHulls[0], root.UnitHulls[1]);

            using (var sw = new StreamWriter(path + name + "/" + bmrkID + "_" + battleName + "_" + randomBattleString + ".txt", true))
            {
                while (TotalIterCount < _maxPlayouts)
                {
                    long timeStamp = Stopwatch.GetTimestamp();

                    ++TotalIterCount;
                    // Selection and Expansion
                    MctsNode selectedNode = SelectAndExpand(root);

                    // Playout
                    GameEnvironment selectedStateClone = selectedNode.CurrentActionState.Environment.DeepCloneState();
                    GameResult result = Game.Playout(selectedStateClone, _players, _playoutRoundLimit, true, relative);
                    double value = EvaluateGame(result, selectedStateClone)/upperBound;

                    // Backpropagation
                    BackpropagateResults(selectedNode, value);

                    if (selectedNode.depth > maxDepth)
                    {
                        maxDepth = selectedNode.depth;
                    }

                    LogTimeAndDepth(1_000_000_000.0 * (Stopwatch.GetTimestamp() - timeStamp) / Stopwatch.Frequency, sw);
                }
            }

            MctsNode bestNode = SelectBestChild(root, environment.GameState.ActivePlayer, (n, p, b) => n.Value/n.VisitedCount);
            var actions = bestNode.CurrentActionState.PlayerAction;

            return actions;
        }

        // Overridden so that the boolean parameter of the treePolicy is set to false.
        // This ensures that the policy doesn't try to scale the values to between
        // 0 and 1.
        protected override MctsNode SelectBestChild(MctsNode node, int player, Func<MctsNode, int, bool, double> evalFunc)
        {
            double bestScore = 0.0;
            MctsNode bestChild = null;
            foreach (MctsNode child in node.Children)
            {
                double score = evalFunc(child, player, false);
                if (bestChild == null ||
                    (score > bestScore && player == 0) ||
                    (score < bestScore && player == 1))
                {
                    bestScore = score;
                    bestChild = child;
                }
            }

            return bestChild;
        }

        /// <summary>
        /// Sums up all the values in the playout sequence while taking their importance into account.
        /// </summary>
        protected override double EvaluateGame(GameResult result, GameEnvironment environment)
        {
            double res = 0;
            double sumOfWeights = 0;

            for (int i = 0; i < result.IntermediateValues.Count; ++i)
            {
                double weight = (ProbabilityOfEncounter(i, result.IntermediateValues.Count) + ValueOfInformation(i, result.IntermediateValues.Count)) / 2;
                sumOfWeights += weight;
                res += result.IntermediateValues[i] * weight;
            }

            return res / (sumOfWeights);
        }

        /// <summary>
        /// Estimates the importance of a state based on the probability of
        /// it being encountered (the earlier it occurs, the higher).
        /// </summary>
        private double ProbabilityOfEncounter(int index, int length)
        {
            return Math.Min(Math.Pow(poeBase, -index), 10000);
        }

        /// <summary>
        /// Estimates the importance of a state based on the value of the
        /// information contained in it (the later it occurs, the higher).
        /// </summary>
        private double ValueOfInformation(int index, int length)
        {
            return Math.Min(Math.Pow(voiBase, index - length), 10000);
        }

        public override string ToString()
        {
            return $"WP MCTS, voiBase: {voiBase}, poeBase: {poeBase}, normalized: {relative}";
        }
    }
}