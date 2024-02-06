using EmptyKeys.Strategy.Units;

namespace EmptyKeys.Strategy.AI.TaskGeneration
{
    public interface IBehaviorTree
    {
        void Execute(IBehaviorContext context);
    }

    public class AttackBehaviorTree : IBehaviorTree
    {
        private Behavior Behavior { get; }

        public AttackBehaviorTree()
        {
            Behavior = BehaviorsManager.Instance.GetBehavior("TaskAttackSystem");
        }

        public void Execute(IBehaviorContext context)
        {
            Behavior.Behave(context);
        }
    }
}