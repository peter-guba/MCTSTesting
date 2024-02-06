using EmptyKeys.Strategy.Units;

namespace EmptyKeys.Strategy.AI.TaskGeneration
{
    public class UnitTaskPair
    {
        public UnitTaskPair(BaseUnit unit, TaskAI task)
        {
            Unit = unit;
            Task = task;
        }

        public BaseUnit Unit { get; }
        public TaskAI Task { get; }
    }
}