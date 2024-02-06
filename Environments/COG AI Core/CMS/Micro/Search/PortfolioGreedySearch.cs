using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CMS.Actions;
using CMS.GameStateEval;
using CMS.Micro.Scripts;
using CMS.Pathfinding;
using CMS.Players;
using CMS.Playout;
using CMS.Units;
using CMS.Utility;

namespace CMS.Micro.Search
{
    /// <summary>
    /// Portfolio greedy search algorithm as presented
    /// in Churchill et al (2013)
    /// </summary>
    public class PortfolioGreedySearch
    {
        private readonly List<IScript> _portfolio;
        private readonly int _imprCount;
        private readonly int _responseCount;
        private readonly IScript _defaultScript;
        private readonly int _timeLimit;
        private readonly int _maxTurns;
        private readonly IGameStateEvaluator _evaluator;

        /// <summary>
        /// For each given (unit, script) assignment save (playout value, player on move) pair
        /// </summary>
        private readonly Dictionary<ScriptAssignmentList, Tuple<float, int>> _playoutCache;

        public PortfolioGreedySearch(
            List<IScript> portfolio, 
            int imprCount,
            int responseCount, 
            IScript defaultScript, 
            int timeLimit, 
            int maxTurns)
        {
            _portfolio = portfolio;
            _imprCount = imprCount;
            _responseCount = responseCount;
            _defaultScript = defaultScript;
            _timeLimit = timeLimit;
            _maxTurns = maxTurns;
            _playoutCache = new Dictionary<ScriptAssignmentList, Tuple<float, int>>();
            _evaluator = new SimpleGameStateEvaluator();
        }

        public List<ActionCms> GetActions(GameEnvironment environment, int player)
        {
            foreach (Unit value in environment.GameState.OtherPlayerUnits.Values)
            {
                value.Script = _defaultScript;
            }
            GetSeedPlayer(environment, player);
            GetSeedPlayer(environment, GameState.Opo(player));
            Improve(environment, player);
            for (int i = 0; i < _responseCount; ++i)
            {
                Improve(environment, GameState.Opo(player));
                Improve(environment, player);
            }

            _playoutCache.Clear();
            return GenerateMoves(environment);
        }

        private void GetSeedPlayer(GameEnvironment environment, int player)
        {
            float bestValue = float.MinValue;
            IScript bestScript = null;
            foreach (IScript script in _portfolio)
            {
                foreach (Unit unit in environment.GameState.Units[player].Values)
                {
                    unit.Script = script;
                }

                float value = Playout(environment, player);
                if (value > bestValue)
                {
                    bestValue = value;
                    bestScript = script;
                }
            }

            foreach (Unit unit in environment.GameState.Units[player].Values)
            {
                unit.Script = bestScript;
            }
        }

        private void Improve(GameEnvironment environment, int player)
        {
            Stopwatch sw = Stopwatch.StartNew();
            for (int i = 0; i < _imprCount; i++)
            {
                foreach (Unit unit in environment.GameState.Units[player].Values)
                {
                    if (sw.ElapsedMilliseconds > _timeLimit)
                    {
                        //Console.WriteLine("PGS: --- Time limit exceeded!");
                        return;
                    }

                    float bestValue = float.MinValue;
                    IScript bestScript = null;
                    foreach (IScript script in _portfolio)
                    {
                        unit.Script = script;
                        float value = Playout(environment, player);
                        if (value > bestValue)
                        {
                            bestValue = value;
                            bestScript = script;
                        }
                    }
                    unit.Script = bestScript;
                }
            }
        }

        private static List<ActionCms> GenerateMoves(GameEnvironment environment)
        {
            var actions = new List<ActionCms>();

            Logger.Log("--- Script assignment ---");

            var units = environment.GameState.ActivePlayerUnits.Values.ToList();
            foreach (Unit unit in units)
            {
                Logger.Log($"{unit.Position} assigned {unit.Script}");
                IScript script = unit.Script;
                List<Unit> enemyUnits = environment.GameState.OtherPlayerUnits.Values.ToList();
                if (enemyUnits.Count == 0)
                    break;
                actions.Add(script.MakeAction(environment, enemyUnits, unit));
            }

            return actions;
        }

        private float Playout(GameEnvironment environment, int player)
        {
            // Check cache
            var sl = new ScriptAssignmentList();
            for (int i = 0; i < 2; i++)
            {
                sl.AddRange(environment.GameState.Units[i].Values.Select(unit => new Tuple<int, IScript>(unit.GlobalKey, unit.Script)));
            }

            Tuple<float, int> cachedPlayout;
            if (_playoutCache.TryGetValue(sl, out cachedPlayout))
            {
                return player == cachedPlayout.Item2 ? cachedPlayout.Item1 : -cachedPlayout.Item1;
            }

            GameEnvironment envClone = environment.DeepCloneState();
            var players = new List<Player>
            {
                new ScriptExecutor(),
                new ScriptExecutor()
            };
            Game.Playout(envClone, players, _maxTurns);

            float playoutValue = (float)_evaluator.EvaluateGameState(envClone.GameState, player);
            _playoutCache[sl] = new Tuple<float, int>(playoutValue, player);

            return playoutValue;
        }

        private class ScriptAssignmentList : List<Tuple<int, IScript>>, IEquatable<ScriptAssignmentList>
        {
            public bool Equals(ScriptAssignmentList other)
            {
                if (Count != other?.Count)
                    return false;

                for (int i = 0; i < Count; i++)
                {
                    if (!this[i].Equals(other[i]))
                        return false;
                }

                return true;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((ScriptAssignmentList) obj);
            }

            public override int GetHashCode()
            {
                int hash = 17;
                foreach (var script in this)
                {
                    hash = hash * 23 + script.GetHashCode();
                }
                return hash;
            }
        }

        public override string ToString()
        {
            return $"PGS impr: {_imprCount}; resp: {_responseCount}";
        }
    }
}
