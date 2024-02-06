using System.Collections.Generic;
using EmptyKeys.Strategy.AI.Components;
using EmptyKeys.Strategy.Units;

namespace EmptyKeys.Strategy.AI.TaskGeneration
{
    public class ExecuteTasks : BehaviorComponentBase
    {
        public override BehaviorReturnCode Behave(IBehaviorContext context)
        {
            var playerContext = context as PlayerBehaviorContext;
            ICollection<TaskAI> allocation = playerContext?.Allocation;
            if (allocation == null)
                return returnCode = BehaviorReturnCode.Failure;

            foreach (TaskAI task in allocation)
            {
                task.Execute();
            }

            return returnCode = BehaviorReturnCode.Success;
        }
    }
}
