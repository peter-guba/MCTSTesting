using System.Collections.Generic;
using System.Linq;
using EmptyKeys.Strategy.AI.TaskGeneration;
using EmptyKeys.Strategy.Core;
using EmptyKeys.Strategy.Units;

namespace EmptyKeys.Strategy.AI.Components.ConditionsTask
{
    public class IsSquadRegrouped : BehaviorComponentBase
    {
        public override BehaviorReturnCode Behave(IBehaviorContext context)
        {
            var taskContext = context as TaskBehaviorContext;
            ICollection<BaseUnit> units = taskContext?.Units;
            if (units == null)
                return returnCode = BehaviorReturnCode.Failure;

            HexElement target = taskContext.EnvironmentTarget;

            if (target.GetOwner(taskContext.GalaxyInfluence) ==
                taskContext.Task.Target.GetOwner(taskContext.GalaxyInfluence))
            {
                return returnCode = BehaviorReturnCode.Success;
            }

            int totalDistance = 0;
            foreach (var unit in units.OfType<MoveableUnit>())
            {
                if (!unit.IsInWarp)
                    return returnCode = BehaviorReturnCode.Failure;
                totalDistance += HexMap.Distance(target, unit);
            }

            int meanDistance = totalDistance / units.Count();
            if (meanDistance > 20)
                return returnCode = BehaviorReturnCode.Failure;

            return returnCode = BehaviorReturnCode.Success;
        }
    }
}
