using System.Collections.Generic;
using EmptyKeys.Strategy.Core;
using EmptyKeys.Strategy.Units;

namespace EmptyKeys.Strategy.AI.TaskGeneration
{
    public class TaskBehaviorContext : BaseBehaviorContext
    {
        public ICollection<BaseUnit> Units { get; set; }
        public IBehaviorTree BehaviorTree { get; set; }

        public TaskAI Task { get; }

        public Player Player { get; }

        public HexMap GalaxyInfluence => Player.GameSession.Galaxy.EnvironmentInfluenceMap;

        public bool IsAttackInitiated { get; set; }

        public HexElement AttackTarget;

        public TaskBehaviorContext(TaskAI task, Player player)
        {
            Task = task;
            Player = player;
        }
    }
}
