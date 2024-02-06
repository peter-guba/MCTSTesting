using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using EmptyKeys.Strategy.AI.Components;
using EmptyKeys.Strategy.Units;

namespace EmptyKeys.Strategy.AI.TaskGeneration
{
    public class AllocateResources : BehaviorComponentBase
    {
        public override BehaviorReturnCode Behave(IBehaviorContext context)
        {
            var playerContext = context as PlayerBehaviorContext;
            ObservableCollection<BaseUnit> units = playerContext?.Player?.Units;
            if (units == null)
                return returnCode = BehaviorReturnCode.Failure;

            ICollection<TaskAI> tasks = playerContext.Tasks;
            if (tasks == null || tasks.Count == 0)
                return returnCode = BehaviorReturnCode.Failure;

            // Select tasks to execute by allocating units
            TaskAI task = tasks.First();
            task.Units = units.OfType<MoveableUnit>().Where(u => u.CanAttack).ToList<BaseUnit>();

            playerContext.Allocation = new List<TaskAI>();
            if (task.Units.Count > 0)
                playerContext.Allocation.Add(task);
            else
                return returnCode = BehaviorReturnCode.Failure;

            return returnCode = BehaviorReturnCode.Success;
        }
    }
}