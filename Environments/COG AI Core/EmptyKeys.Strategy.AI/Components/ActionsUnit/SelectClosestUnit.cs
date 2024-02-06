using System.Linq;
using EmptyKeys.Strategy.Core;
using EmptyKeys.Strategy.Units;

namespace EmptyKeys.Strategy.AI.Components.ActionsUnit
{
    public class SelectClosestUnit : BehaviorComponentBase
    {
        public override BehaviorReturnCode Behave(IBehaviorContext context)
        {
            var unitContext = context as UnitBehaviorContext;
            var targets = unitContext?.Units;
            var unit = unitContext?.Unit as StaticUnit;
            if (targets == null || unit == null)
            {
                returnCode = BehaviorReturnCode.Failure;
                return returnCode;
            }

            BaseUnit closestUnit = null;
            int smallestDistance = int.MaxValue;
            foreach (var target in targets.OfType<StaticUnit>())
            {
                int distance = HexMap.Distance(target, unit);
                if (distance < smallestDistance)
                {
                    closestUnit = target;
                    smallestDistance = distance;
                }
            }

            unit.Target = closestUnit;

            returnCode = BehaviorReturnCode.Success;
            return returnCode;
        }
    }
}
