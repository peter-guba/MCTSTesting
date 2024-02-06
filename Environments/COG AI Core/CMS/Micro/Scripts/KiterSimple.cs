using System;
using System.Collections.Generic;
using CMS.Actions;
using CMS.Units;

namespace CMS.Micro.Scripts
{
    [Obsolete("Script needs to be tested and debugged first", true)]
    public class KiterSimple : IScript
    {
        public ActionCms MakeAction(GameEnvironment environment, IEnumerable<Unit> enemyUnits, Unit unit)
        {
            var actions = new List<ActionCms>();
            HexMap<MicroHelpers.UnitValue> unitValues = MicroHelpers.CalculateUnitValues(enemyUnits);
            Unit bestValueUnit = MicroHelpers.FindBestValueUnitInRange(unit, unitValues);
            if (bestValueUnit != null)
            {
                // Attack
                var attackAction = new AttackAction(unit, bestValueUnit.Position);
                attackAction.Execute(environment);
                actions.Add(attackAction);

                RunAway(unit, enemyUnits, environment, actions);
            }
            else // No unit in range - go to closest
            {
                Unit closestUnit = MicroHelpers.GetClosestUnit(unit.Position, enemyUnits);

                var moveAction = new MoveAction(unit, closestUnit.Position, MicroHelpers.FindPosPrefWepRangeCond);
                moveAction.Execute(environment);
                actions.Add(moveAction);

                // Try attacking again
                bestValueUnit = MicroHelpers.FindBestValueUnitInRange(unit, unitValues);
                if (bestValueUnit != null)
                {
                    var attackAction = new AttackAction(unit, bestValueUnit.Position);
                    attackAction.Execute(environment);
                    actions.Add(attackAction);

                    RunAway(unit, enemyUnits, environment, actions);
                }
            }

            return new CompositeAction(unit, actions);
        }

        public string ToShortName()
        {
            return "k";
        }

        private static void RunAway(Unit unit, IEnumerable<Unit> enemyUnits, GameEnvironment environment, ICollection<ActionCms> actions)
        {
            // Run away
            if (unit.CanMove)
            {
                Unit closestEnemy = MicroHelpers.GetClosestUnit(unit.Position, enemyUnits);
                if (closestEnemy == null) // No one to rune from
                {
                    return;
                }
                Hex runawayDir = Constants.HexDirections[Hex.GetReverseDirection(unit.Position.GetDirection(closestEnemy.Position))];
                Hex runawayPos = unit.Position + runawayDir * (short)unit.AvailEnginesEnergy;
                Hex? goTo = MicroHelpers.FindPosNear(runawayPos, unit, environment, 1);
                if (goTo != null)
                {
                    var moveAction = new MoveAction(unit, goTo.Value);
                    moveAction.Execute(environment);
                    actions.Add(moveAction);
                }
            }
        }

        protected bool Equals(KiterSimple other)
        {
            return true;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Kiter) obj);
        }

        public override int GetHashCode()
        {
            return 4;
        }
    }
}
