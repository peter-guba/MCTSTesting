using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CMS.Actions;
using CMS.Micro.Scripts;
using CMS.Units;

namespace CMS.ActionGenerators
{
    public class ScriptActionGenerator : IActionGenerator
    {
        private static Random rand = new Random();

        private readonly List<IScript> _scripts;

        public ScriptActionGenerator(List<IScript> scripts)
        {
            _scripts = scripts;
        }

        public List<ActionStatePair> GenerateActions(GameEnvironment environment)
        {
            var unitCount = environment.GameState.ActivePlayerUnits.Count;
            return GenAllActions(0, unitCount, new List<ActionCms>(), environment).ToList();
        }

        public IEnumerable<ActionStatePair> EnumerateActions(GameEnvironment environment)
        {
            var unitCount = environment.GameState.ActivePlayerUnits.Count;
            foreach (ActionStatePair playerAction in GenAllActions(0, unitCount, new List<ActionCms>(), environment))
            {
                yield return playerAction;
            }
        }

        public int GetActionCount(GameEnvironment environment)
        {
            return (int) Math.Pow(_scripts.Count, environment.GameState.ActivePlayerUnits.Count);
        }

        private IEnumerable<ActionStatePair> GenAllActions(int unitIdx, int unitCount, IList<ActionCms> current, GameEnvironment environment)
        {
            if (unitIdx == unitCount || 
                environment.GameState.GetResult() != GameState.WINNER_NONE)
            {
                yield return new ActionStatePair {PlayerAction = new List<ActionCms>(current), Environment = environment};
            }
            else
            {
                int count = _scripts.Count;
                List<int> sequence = new List<int>();
                for (int i = 0; i < _scripts.Count; ++i)
                {
                    sequence.Add(i);
                }

                for (int i = count; i > 0; --i)
                {
                    int index = rand.Next(i);
                    IScript script = _scripts[sequence[index]];
                    sequence.RemoveAt(index);

                    GameEnvironment envClone = environment.DeepCloneState();
                    IEnumerable<Unit> enemyUnits = envClone.GameState.OtherPlayerUnits.Values;
                    List<Unit> units = envClone.GameState.ActivePlayerUnits.Values.ToList();

                    ActionCms action = script.MakeAction(envClone, enemyUnits, units[unitIdx]);
                    units[unitIdx].Script = script;
                    current.Add(action);

                    foreach (ActionStatePair ret in GenAllActions(unitIdx + 1, unitCount, current, envClone))
                    {
                        yield return ret;
                    }
                    current.RemoveAt(current.Count - 1);
                }
            }
        }
    }
}
