using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CMS.ActionGenerators;
using CMS.Actions;
using CMS.Micro.Scripts;
using CMS.Pathfinding;
using CMS.Units;
using CMS.Utility;

namespace CMS.Players.Script
{
    /// <inheritdoc />
    /// <summary>
    /// A <see cref="Player"/> which executes <see cref="TScript"/> script for all units.
    /// </summary>
    /// <typeparam name="TScript">Script to execute for all units.</typeparam>
    public class ScriptedPlayer<TScript> : Player
        where TScript : IScript, new()
    {
        private readonly TScript _script;

        public ScriptedPlayer()
        {
            _script = new TScript();
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
                    Logger.Log($"Scripted Player: No enemy units found");
                    return actions;
                }

                actions.Add(_script.MakeAction(environment, enemyUnits, unit));
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