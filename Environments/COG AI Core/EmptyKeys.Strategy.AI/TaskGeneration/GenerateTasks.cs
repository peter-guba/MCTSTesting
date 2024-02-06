using System.Collections.Generic;
using EmptyKeys.Strategy.AI.TaskGeneration;
using System.Linq;
using EmptyKeys.Strategy.Core;
using EmptyKeys.Strategy.Environment;

namespace EmptyKeys.Strategy.AI.Components.ActionsPlayer
{
    public class GenerateTasks : BehaviorComponentBase
    {
        public override BehaviorReturnCode Behave(IBehaviorContext context)
        {
            var playerContext = context as PlayerBehaviorContext;
            var generators = playerContext?.TaskGenerators;
            if (generators == null)
                return returnCode = BehaviorReturnCode.Failure;

            var tasks = new List<TaskAI>();
            foreach (ITaskGenerator taskGenerator in generators)
            {
                tasks.AddRange(taskGenerator.GenerateTasks());
            }

            playerContext.Tasks = tasks;

            return returnCode = BehaviorReturnCode.Success;
        }
    }
}
