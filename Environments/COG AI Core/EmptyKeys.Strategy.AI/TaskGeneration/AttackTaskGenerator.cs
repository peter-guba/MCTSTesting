using System;
using System.Collections.Generic;
using System.Linq;
using EmptyKeys.Strategy.Core;
using EmptyKeys.Strategy.Environment;
using EmptyKeys.Strategy.Units;

namespace EmptyKeys.Strategy.AI.TaskGeneration
{
    class AttackTaskGenerator : ITaskGenerator
    {
        private Player Player { get; }
        private IBehaviorTree BehaviorTree { get; }

        public AttackTaskGenerator(Player player)
        {
            Player = player;
            BehaviorTree = new AttackBehaviorTree();
        }

        public IEnumerable<TaskAI> GenerateTasks()
        {
            var tasks = new List<TaskAI>();

            foreach (BaseEnvironment system in Helpers.GetEnemySystems(Player))
            {
                IEnumerable<StaticUnit> units = system.UnitsMap.Values.OfType<StaticUnit>().Where(u => u.Owner != Player);
                var task = new TaskAI(BehaviorTree, system, BattleEstimator.EstimateStrength(units), Player);
                task.Priority = CalculatePriority(task);
                tasks.Add(task);
            }

            PruneTasks(tasks);

            return tasks;
        }

        private float CalculatePriority(TaskAI task)
        {
            float priority = 0.0f;
            foreach (BaseEnvironment system in Helpers.GetOwnedSystems(Player))
            {
                priority -= (float)Math.Pow(HexMap.Distance(task.Target, system), 2);
            }
            return priority;
        }

        private void PruneTasks(List<TaskAI> tasks)
        {
            float playerStrength = BattleEstimator.EstimateStrength(Player.Units.OfType<StaticUnit>());
            tasks.RemoveAll(t => t.RequiredStrength > playerStrength);
        }
    }
}