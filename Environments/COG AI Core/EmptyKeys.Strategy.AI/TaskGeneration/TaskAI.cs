using System;
using System.Collections.Generic;
using EmptyKeys.Strategy.Core;
using EmptyKeys.Strategy.Units;

namespace EmptyKeys.Strategy.AI.TaskGeneration
{
    public class TaskAI
    {
        private ICollection<BaseUnit> _units;
        public ICollection<BaseUnit> Units
        {
            get { return _units; }
            set
            {
                _units = value;
                BehaviorContext.Units = value;
            }
        }

        private readonly IBehaviorTree BehaviorTree;

        public HexElement Target { get; }
        private readonly TaskBehaviorContext BehaviorContext;

        public float Priority { get; set; }
        public float RequiredStrength { get; }

        public TaskAI(IBehaviorTree behaviorTree, HexElement target, float requiredStrength, Player player)
        {
            BehaviorTree = behaviorTree;
            Target = target;
            RequiredStrength = requiredStrength;
            BehaviorContext = new TaskBehaviorContext(this, player);
        }

        public void Execute()
        {
            BehaviorTree.Execute(BehaviorContext);
        }
    }
}