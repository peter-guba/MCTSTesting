using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CMS.Actions;
using CMS.Micro.Scripts;
using CMS.Units;

namespace CMS.Players.Script
{
    /// <inheritdoc />
    /// <summary>
    /// A <see cref="Player"/> which executes random scripts from a given set of scripts.
    /// </summary>
    public class RandomScriptSelectPlayer : Player
    {
        private readonly List<IScript> _scripts;
        private readonly Random _rnd;

        /// <param name="scripts">Set of scripts to randomly select from.</param>
        public RandomScriptSelectPlayer(List<IScript> scripts)
        {
            _scripts = scripts;
            _rnd = new Random();
        }

        public override ICollection<ActionCms> MakeActions(GameEnvironment environment)
        {
            var actions = new List<ActionCms>();
            var myUnits = environment.GameState.ActivePlayerUnits.Values.ToList();

            foreach (Unit unit in myUnits)
            {
                List<Unit> enemyUnits = environment.GameState.OtherPlayerUnits.Values.ToList();
                if (enemyUnits.Count == 0)
                {
                    return actions;
                }
                int rndInx = _rnd.Next(_scripts.Count);
                actions.Add(_scripts[rndInx].MakeAction(environment, enemyUnits, unit));
            }

            return actions;
        }

        public override void SetBattleName(string name)
        {
            throw new NotImplementedException();
        }

        public override void SetRndBattleString(string str)
        {
            throw new NotImplementedException();
        }
    }
}

