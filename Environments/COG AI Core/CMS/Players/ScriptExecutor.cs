using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CMS.Actions;
using CMS.Units;

namespace CMS.Players
{
    /// <inheritdoc />
    /// <summary>
    /// Simple <see cref="Player"/> implementation which executes scripts 
    /// saved in units for all units of current player.
    /// </summary>
    internal class ScriptExecutor : Player
    {
        public override ICollection<ActionCms> MakeActions(GameEnvironment environment)
        {
            List<Unit> units = environment.GameState.ActivePlayerUnits.Values.ToList();
            foreach (Unit unit in units)
            {
                List<Unit> enemyUnits = environment.GameState.OtherPlayerUnits.Values.ToList();
                if (enemyUnits.Count == 0)
                    return null;
                unit.Script.MakeAction(environment, enemyUnits, unit);
            }

            return null;
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