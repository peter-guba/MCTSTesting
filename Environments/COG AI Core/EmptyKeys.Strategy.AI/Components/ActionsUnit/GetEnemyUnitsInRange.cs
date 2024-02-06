using EmptyKeys.Strategy.Core;
using EmptyKeys.Strategy.Units;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EmptyKeys.Strategy.Environment;

namespace EmptyKeys.Strategy.AI.Components.ActionsUnit
{
    public class GetEnemyUnitsInRange : BehaviorComponentBase
    {
        public override BehaviorReturnCode Behave(IBehaviorContext context)
        {
            var unitContext = context as UnitBehaviorContext;
            var unit = unitContext?.Unit as StaticUnit;
            BaseEnvironment envi = unit?.Environment;
            if (envi == null)
            {
                returnCode = BehaviorReturnCode.Failure;
                return returnCode;
            }

            Player player = unit.Owner;
            var unitsInRange = new List<BaseUnit>();
            foreach (var target in envi.UnitsMap.Values.OfType<BaseUnit>())
            {
                if (target?.Owner == null || target.Owner == player || target.IsDead)
                    continue;

                //if (!(player.IsHostile(target.Owner) || target.Owner.IsHostile(player)))
                //    continue;

                int targetDistance = HexMap.Distance(unit, target);
                if (targetDistance <= unit.SensorsEnergy)
                {
                    unitsInRange.Add(target);
                }
            }

            if (unitsInRange.Count > 0)
            {
                unitContext.Units = unitsInRange;

                returnCode = BehaviorReturnCode.Success;
                return returnCode;
            }

            returnCode = BehaviorReturnCode.Failure;
            return returnCode;
        }
    }
}
