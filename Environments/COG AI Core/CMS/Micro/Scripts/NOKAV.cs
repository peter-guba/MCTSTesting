using System.Collections.Generic;
using CMS.Actions;
using CMS.Pathfinding;
using CMS.Units;
using CMS.Utility;

namespace CMS.Micro.Scripts
{
    /// <inheritdoc />
    /// <summary>
    /// An <see cref="IScript" /> implementing the "No OverKill Attack Value" behavior.
    /// </summary>
    public class NOKAV : IScript
    {
        public ActionCms MakeAction(GameEnvironment environment, IEnumerable<Unit> enemyUnits, Unit unit)
        {
            var actions = new List<ActionCms>();
            HexMap<MicroHelpers.UnitValue> unitValues = MicroHelpers.CalculateUnitValues(enemyUnits);
            Unit bestValueUnit = MicroHelpers.FindBestValueUnitInRange(unit, unitValues);
            if (bestValueUnit != null)
            {
                var attackAction = new AttackAction(unit, bestValueUnit.Position);
                attackAction.Execute(environment);
                actions.Add(attackAction);
            }
            else // No unit in range - go to closest
            {
                Unit closestUnit = MicroHelpers.GetClosestUnit(unit.Position, enemyUnits);
                Hex? posNear = MicroHelpers.FindPosNearPrefWepRange(closestUnit.Position, unit, environment);
                if (posNear == null)
                {
                    Logger.Log($"NOKAV Position close to {closestUnit.Position} not found");
                    return new CompositeAction(unit, actions);
                }

                var moveAction = new MoveAction(unit, posNear.Value);
                moveAction.Execute(environment);
                actions.Add(moveAction);

                // Try attacking again
                bestValueUnit = MicroHelpers.FindBestValueUnitInRange(unit, unitValues);
                if (bestValueUnit != null)
                {
                    var attackAction = new AttackAction(unit, bestValueUnit.Position);
                    attackAction.Execute(environment);
                    actions.Add(attackAction);
                }
            }

            return new CompositeAction(unit, actions);
        }

        public string ToShortName()
        {
            return "N";
        }

        protected bool Equals(NOKAV other)
        {
            return true;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((NOKAV) obj);
        }

        public override int GetHashCode()
        {
            return 2;
        }
    }
}
