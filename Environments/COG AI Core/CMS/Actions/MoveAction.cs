using System;
using System.Collections.Generic;
using CMS.Pathfinding;
using CMS.Units;
using CMS.Utility;

namespace CMS.Actions
{
    public class MoveAction : ActionCms
    {
        public Hex Target { get; }
        private readonly Pathfinder.AStarStopCondition _condition;

        public MoveAction(Unit u, Hex target, Pathfinder.AStarStopCondition condition = null)
            : base(u)
        {
            Target = target;
            _condition = condition;
        }

        public override string ToString()
        {
            return $"Move action from: {Source.ToString()} to: {Target.ToString()}";
        }

        public IReadOnlyList<PathNode> GetPath(GameEnvironment environment)
        {
            var pathfinder = new Pathfinder(environment);

            return pathfinder.FindPath(Source, Target, _condition);
        }

        public override void Execute(GameEnvironment environment)
        {
            Unit unit = environment.GameState.GetUnitAt(Source);
            if (!unit.CanMove)
                return;

            if (Source != unit.Position)
            {
                Logger.Log($"ERROR: Move action - Unit {unit.Position} not at source position {Source}");
                throw new Exception("Move action error");
            }

            // If path not cached, calculate and cache one
            var path = GetPath(environment);

            if (path.Count == 0)
            {
                Logger.Log($"Path not found from {Source} to {Target}");
                return;
            }

            if (path.Count == 1)
            {
                // Start = end -> no reason to follow this path
                return;
            }

            float availEnginesEnergy = unit.AvailEnginesEnergy;

            int i;
            for (i = path.Count - 1; i >= 0; --i)
            {
                if (availEnginesEnergy - path[i].Cost >= 0.0)
                {
                    availEnginesEnergy -= path[i].Cost;
                }
                else
                {
                    break;
                }
            }

            if (i == path.Count - 1) // We did not move
                return;

            if (i < 0) // We went all the way
                i = 0;

            environment.GameState.MoveUnit(unit.Position, path[i].Hex, unit);
            unit.Direction = path[i + 1].Hex.GetDirection(path[i].Hex);
            unit.AvailEnginesEnergy = availEnginesEnergy;
        }
    }
}
