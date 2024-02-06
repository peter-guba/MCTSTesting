using EmptyKeys.Strategy.AI.TaskGeneration;

namespace EmptyKeys.Strategy.AI.Components.ActionsTask
{
    public class FindAttackTarget : BehaviorComponentBase
    {
        public override BehaviorReturnCode Behave(IBehaviorContext context)
        {
            var taskContext = context as TaskBehaviorContext;
            if (taskContext == null)
                return returnCode = BehaviorReturnCode.Failure;

            taskContext.AttackTarget = taskContext.Task.Target;

            return returnCode = BehaviorReturnCode.Success;
        }
    }
}
