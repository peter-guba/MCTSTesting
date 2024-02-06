using EmptyKeys.Strategy.Core;
using EmptyKeys.Strategy.Units;
using System;
using System.Collections.Generic;

namespace EmptyKeys.Strategy.AI.Components.ActionsUnit
{
    public class GetAllEnemyUnits : BehaviorComponentBase
    {
        public override BehaviorReturnCode Behave(IBehaviorContext context)
        {
            var unitContext = context as UnitBehaviorContext;
            var unit = unitContext?.Unit as StaticUnit;
            var envi = unit?.Environment;
            if (envi == null)
            {
                returnCode = BehaviorReturnCode.Failure;
                return returnCode;
            }

            Player player = unit.Owner;
            var unitsInSystem = new List<BaseUnit>();
            foreach (var element in envi.UnitsMap.Values)
            {
                var target = element as BaseUnit;
                if (target?.Owner == null || target.Owner == player || target.IsDead)
                    continue;

                //if (!(player.IsHostile(target.Owner) || target.Owner.IsHostile(player)))
                //    continue;

                unitsInSystem.Add(target);
            }

            unitContext.Units = unitsInSystem;

            returnCode = BehaviorReturnCode.Success;
            return returnCode;
        }
    }
}