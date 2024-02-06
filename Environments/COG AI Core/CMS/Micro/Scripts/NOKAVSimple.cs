using System;
using System.Collections.Generic;
using CMS.Actions;
using CMS.Units;

namespace CMS.Micro.Scripts
{
    [Obsolete("Script needs to be tested and debugged first", true)]
    public class NOKAVSimple : IScript
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
                }
            }

            return new CompositeAction(unit, actions);
        }

        public string ToShortName()
        {
            return "n";
        }

        protected bool Equals(NOKAVSimple other)
        {
            return true;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((NOKAV)obj);
        }

        public override int GetHashCode()
        {
            return 3;
        }
    }
}
