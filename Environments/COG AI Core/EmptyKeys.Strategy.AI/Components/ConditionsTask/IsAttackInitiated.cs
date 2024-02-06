using EmptyKeys.Strategy.AI.TaskGeneration;

namespace EmptyKeys.Strategy.AI.Components.ConditionsTask
{
    public class IsAttackInitiated : BehaviorComponentBase
    {
        public override BehaviorReturnCode Behave(IBehaviorContext context)
        {
            var taskContext = context as TaskBehaviorContext;
            if (taskContext == null)
                return returnCode = BehaviorReturnCode.Failure;

            if (taskContext.IsAttackInitiated)
                return returnCode = BehaviorReturnCode.Success;

            return returnCode = BehaviorReturnCode.Failure;
        }
    }
}