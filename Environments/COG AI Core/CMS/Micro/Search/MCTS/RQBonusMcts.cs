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
    /// A variant that applies a bonus to the result of a playout based on its length and/or the quality of the result.
    /// </summary>
    public class RQBonusMcts : Mcts
    {
        /// <summary>
        /// A constant used in computing the sigmoid function of the bonuses.
        /// </summary>
        private readonly double _k = 1.0;

        /// <summary>
        /// The simulation distance from the root to the node created in the expansion phase.
        /// </summary>
        private int _traversalDistance = 0;

        /// <summary>
        /// The average combined distance of the partial game tree traversal and the playout during
        /// an iteration.
        /// </summary>
        private double _averageDistance = 0;

        /// <summary>
        /// The sample standard deviation of the sampled simulation lengths.
        /// </summary>
        private double _sampleStandardRelativeDeviation = 0;

        /// <summary>
        /// Like averageDistance, but all the lengths of playouts where player 0 lost get substituted with zeros.
        /// </summary>
        private double _averageWinDistance = 0;

        /// <summary>
        /// A list of all the simulation distances encountered so far.
        /// Has to be kept for the sample standard deviation to be computable.
        /// </summary>
        private List<double> _distances = new List<double>();

        /// <summary>
        /// A list of all the final state qualities encountered so far.
        /// Has to be kept for the sample standard deviation to be computable.
        /// </summary>
        private List<double> _qualities = new List<double>();

        /// <summary>
        /// A list of the results of all the playouts until now. Used when computing alpha.
        /// </summary>
        private List<double> _playoutResults = new List<double>();

        /// <summary>
        /// The average quality of final states encountered in playouts.
        /// </summary>
        private double _averageQuality = 0;

        /// <summary>
        /// The sample standard deviation of the sampled simulation lengths.
        /// </summary>
        private double _sampleStandardQualitativeDeviation = 0;

        /// <summary>
        /// Like averageQuality, but all the lengths of playouts where player 0 lost get substituted with zeros.
        /// </summary>
        private double _averageWinQuality = 0;

        /// <summary>
        /// Determines whether the relative bonus is supposed to be added to the result.
        /// </summary>
        private bool relativeBonusEnabled;

        /// <summary>
        /// Determines whether the qualitative bonus is supposed to be added to the result.
        /// </summary>
        private bool qualitativeBonusEnabled;

        public RQBonusMcts(
            IActionGenerator actionGenerator,
            int maxPlayouts,
            List<Player> players,
            double k,
            string bmrkID,
            int playoutRoundLimit = int.MaxValue,
            bool relativeBonusEnabled = false,
            bool qualitativeBonusEnabled = false) : base(actionGenerator, maxPlayouts, players, bmrkID, playoutRoundLimit, relativeBonusEnabled ? qualitativeBonusEnabled ? "rq_bonus_mcts" : "r_bonus_mcts" : "q_bonus_mcts")
        {
            _k = k;
            this.relativeBonusEnabled = relativeBonusEnabled;
            this.qualitativeBonusEnabled = qualitativeBonusEnabled;
        }

        // Overriden so that the appropriate values are reset between moves.
        internal override ICollection<ActionCms> GetActions(GameEnvironment environment)
        {
            // Reset variables between moves.
            _averageDistance = 0;
            _sampleStandardRelativeDeviation = 0;
            _averageWinDistance = 0;
            _distances = new List<double>();
            _qualities = new List<double>();
            _playoutResults = new List<double>();
            _averageQuality = 0;
            _sampleStandardQualitativeDeviation = 0;
            _averageWinQuality = 0;

            TotalIterCount = 0;
            maxDepth = 0;
            var rootActionState = new ActionStatePair { Environment = environment };
            IEnumerable<ActionStatePair> rootChildActions = _actionGenerator.EnumerateActions(rootActionState.Environment);

            var root = new MctsNode(null, rootActionState, rootChildActions, 0);

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

            MctsNode bestNode = SelectBestChild(root, environment.GameState.ActivePlayer, (n, p, b) => p == 0 ? n.Value / n.VisitedCount : -n.Value / n.VisitedCount);
            var actions = bestNode.CurrentActionState.PlayerAction;

            return actions;
        }

        // Overridden so that the depth of the tree can be measured using the _traversalDistance variable.
        protected override MctsNode SelectAndExpand(MctsNode node)
        {
            Debug.Assert(node != null);
            _traversalDistance = 0;

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

                // Find child node with highest value and call Selection recursively
                int player = node.CurrentActionState.Environment.GameState.ActivePlayer;
                MctsNode bestChild = SelectBestChild(node, player, Ucb);

                if (bestChild == null)
                    return null;

                node = bestChild;
                _traversalDistance++;
            }
        }

        // Adds a bonus to the default 1, 0, -1 evaluation.
        protected override double EvaluateGame(GameResult result, GameEnvironment environment)
        {
            double reward;

            if (result.Winner == GameState.WINNER_NONE || result.IsDraw)
            {
                reward = 0.0;
            }
            else if (result.Winner == 0)
            {
                reward = 1.0;
            }
            else
            {
                // We lost or are in bad position
                reward = -1.0;
            }

            _playoutResults.Add(reward);

            if (relativeBonusEnabled)
            {
                int distance = _traversalDistance + result.RoundCount;

                if (_sampleStandardRelativeDeviation > 0)
                {
                    // Compute the relative bonus based on simulation length.
                    double lambdaR = Bonus(_averageDistance - (distance), _sampleStandardRelativeDeviation);
                    double alpha = GetAlpha(_averageDistance, _averageWinDistance, _distances, _sampleStandardRelativeDeviation);
                    reward += reward * alpha * lambdaR;
                }

                // Update the list of encountered distances, the mean, the mean of winning distances and the sample standard deviation.
                _distances.Add(distance);
                _averageDistance = (_averageDistance * (TotalIterCount - 1) + distance) / TotalIterCount;
                _averageWinDistance = (_averageWinDistance * (TotalIterCount - 1) + Math.Max(0, Math.Sign(reward)) * distance) / TotalIterCount;

                if (TotalIterCount > 1)
                {
                    _sampleStandardRelativeDeviation = 0;
                    foreach (int d in _distances)
                    {
                        _sampleStandardRelativeDeviation += (d - _averageDistance) * (d - _averageDistance);
                    }
                    _sampleStandardRelativeDeviation = (float)Math.Sqrt(_sampleStandardRelativeDeviation / (TotalIterCount - 1));
                }
            }

            // Compute qualitative bonus, if it is enabled.
            if (qualitativeBonusEnabled)
            {
                double quality = environment.GameState.Units[0].Values.Sum(x => x.Hull)
                           - environment.GameState.Units[1].Values.Sum(x => x.Hull);

                if (_sampleStandardQualitativeDeviation > 0)
                {
                    // Compute the qualitative bonus based on simulation length.
                    double lambdaR = Bonus(quality - _averageQuality, _sampleStandardQualitativeDeviation);
                    reward += reward * GetAlpha(_averageQuality, _averageWinQuality, _qualities, _sampleStandardQualitativeDeviation) * lambdaR;
                }

                // Update the list of encountered qualities, the mean, the mean of winning qualities and the sample standard deviation.
                _qualities.Add(quality);
                _averageQuality = (_averageQuality * (TotalIterCount - 1) + quality) / TotalIterCount;
                _averageWinQuality = (_averageQuality * (TotalIterCount - 1) + Math.Max(0, Math.Sign(reward)) * quality) / TotalIterCount;

                if (TotalIterCount > 1)
                {
                    _sampleStandardQualitativeDeviation = 0;
                    foreach (float q in _qualities)
                    {
                        _sampleStandardQualitativeDeviation += (q - _averageQuality) * (q - _averageQuality);
                    }
                    _sampleStandardQualitativeDeviation = (float)Math.Sqrt(_sampleStandardQualitativeDeviation / (TotalIterCount - 1));
                }
            }

            return reward;
        }

        public override string ToString()
        {
            return $"RQ-Bonus MCTS, k: {_k}, relative bonus: {relativeBonusEnabled}, qualitative bonus: {qualitativeBonusEnabled}, playouts: {_maxPlayouts}";
        }

        /// <summary>
        /// Computes the bonus from the sample standard deviation and offset from the mean.
        /// </summary>
        private double Bonus(double offsetFromMean, double sampleStandardDeviation)
        {
            double lambda = offsetFromMean / sampleStandardDeviation;
            return -1 + 2 / (1 + Math.Exp(-_k * lambda));
        }

        /// <summary>
        /// Computes the alpha multiplier used when computing the bonus.
        /// </summary>
        private double GetAlpha(double averageMetric, double averageWinMetric, List<double> metricList, double sampleStandardDeviation)
        {
            double covariance = 0;
            for (int i = 0; i < metricList.Count; ++i)
            {
                double winM = Math.Max(0, _playoutResults[i]) * metricList[i];
                covariance += (winM - averageWinMetric) * (metricList[i] - averageMetric);
            }
            covariance /= TotalIterCount - 1;

            return Math.Abs(covariance / sampleStandardDeviation);
        }
    }
}
