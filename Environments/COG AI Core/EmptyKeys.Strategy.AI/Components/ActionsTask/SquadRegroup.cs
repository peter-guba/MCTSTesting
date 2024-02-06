using System.Linq;
using EmptyKeys.Strategy.AI.TaskGeneration;
using EmptyKeys.Strategy.Units;

namespace EmptyKeys.Strategy.AI.Components.ActionsTask
{
    public class SquadRegroup : BehaviorComponentBase
    {
        public override BehaviorReturnCode Behave(IBehaviorContext context)
        {
            var taskContext = context as TaskBehaviorContext;
            var units = taskContext?.Units.OfType<MoveableUnit>();
            if (units == null)
                return returnCode = BehaviorReturnCode.Failure;

            foreach (MoveableUnit unit in units)
            {
                if (unit.Behavior.Name != "UnitRegroup")
                {
                    // Switch based on unit type etc.
                    unit.Behavior = BehaviorsManager.Instance.GetBehavior("UnitRegroup");
                    unit.BehaviorContext.EnvironmentTarget = taskContext.EnvironmentTarget;
                }
            }

            return returnCode = BehaviorReturnCode.Success;
        }
    }
}