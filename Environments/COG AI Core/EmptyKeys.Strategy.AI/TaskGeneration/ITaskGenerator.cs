using System.Collections.Generic;

namespace EmptyKeys.Strategy.AI.TaskGeneration
{
    public interface ITaskGenerator
    {
        IEnumerable<TaskAI> GenerateTasks();
    }
}
