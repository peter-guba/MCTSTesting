using System.Collections.Generic;
using System.Linq;
using CMS.Units;

namespace CMS.Micro.Scripts
{
    /// <summary>
    /// A set of utility methods for micromanagement.
    /// </summary>
    internal static class MicroHelpers
    {
        /// <summary>
        /// Calculates attac values of <paramref name="units"/>.
        /// </summary>
        /// <param name="units">Units to calculate values of.</param>
        /// <returns>Position-value mapping of given <paramref name="units"/></returns>
        public static HexMap<UnitValue> CalculateUnitValues(IEnumerable<Unit> units)
        {
            var values = new HexMap<UnitValue>();
            foreach (Unit unit in units)
            {
                values.Add(unit.Position, new UnitValue(unit, GetUnitValue(unit)));
            }

            return values;
        }
        
        /// <summary>
        /// Finds a unit with the best value in weapons' range.
        /// </summary>
        /// <param name="unit"><see cref="Unit"/> according to which the best unit is found.</param>
        /// <param name="unitValues">Units and values we want to search in.</param>
        /// <returns>A <see cref="Unit"/> with the best value in weapons' range.</returns>
        public static Unit FindBestValueUnitInRange(Unit unit, HexMap<UnitValue> unitValues)
        {
            UnitValue bestValueUnit = null;
            foreach (UnitValue enemyUnit in unitValues.Values)
            {
                if ((bestValueUnit == null || 
                    bestValueUnit.Value < enemyUnit.Value) &&
                    unit.Position.GetDistance(enemyUnit.Unit.Position) <= unit.SensorsEnergy)
                {
                    bestValueUnit = enemyUnit;
                }
            }

            return bestValueUnit?.Unit;
        }
        
        /// <summary>
        /// Finds position near <paramref name="target"/> which is preferable in weapons' range of <paramref name="unit"/>.
        /// </summary>
        /// <param name="target">Position near this target to find.</param>
        /// <param name="unit">Unit for which we are finding the position.</param>
        /// <param name="environment">Context in which we operate.</param>
        /// <returns>Position near given taget. Null if no such position exists.</returns>
        public static Hex? FindPosNearPrefWepRange(Hex target, Unit unit, GameEnvironment environment)
        {
            var wep = FindPosInWeaponsRange(target, unit, environment);
            if (wep != null)
                return wep;

            return FindPosNear(target, unit, environment, (short)unit.SensorsEnergy);
        }

        /// <inheritdoc cref="FindPosNearPrefWepRange"/>
        /// <summary>
        /// Finds position near <paramref name="target"/> weapons' range of <paramref name="unit"/>.
        /// </summary>
        public static Hex? FindPosInWeaponsRange(Hex target, Unit unit, GameEnvironment environment)
        {
            Hex? value = null;
            short radius = (short)unit.SensorsEnergy;
            var closestDist = int.MaxValue;

            var ring = target.GetRing(radius);
            foreach (Hex newPos in ring)
            {
                var distance = unit.Position.GetDistance(newPos);
                if (environment.IsPassable(newPos) &&
                    distance <= unit.AvailEnginesEnergy &&
                    (value == null || distance < closestDist))
                {
                    value = newPos;
                    closestDist = distance;
                }
            }

            return value;
        }

        /// <inheritdoc cref="FindPosNearPrefWepRange"/>
        /// <summary>
        /// Finds position near <paramref name="target"/>.
        /// </summary>
        public static Hex? FindPosNear(Hex target, Unit unit, GameEnvironment environment, short radius)
        {
            if (environment.IsPassable(target))
                return target;

            Hex? value = null;
            var unitToTargetDist = unit.Position.GetDistance(target);
            while (radius < unitToTargetDist && 
                value == null)
            {
                var ring = target.GetRing(radius);
                foreach (Hex newPos in ring)
                {
                    if (environment.IsPassable(newPos) && 
                        unit.Position.GetDistance(newPos) <= unit.AvailEnginesEnergy)
                    {
                        value = newPos;
                        break;
                    }
                }
                ++radius;
            }

            return value;
        }

        /// <summary>
        /// From a set of <paramref name="units"/> find the one closest to <paramref name="position"/>.
        /// </summary>
        /// <returns>A <see cref="Unit"/> from <paramref name="units"/> closest to <paramref name="position"/>.</returns>
        public static Unit GetClosestUnit(Hex position, IEnumerable<Unit> units)
        {
            Unit closest = null;
            var closestDistance = int.MaxValue;
            foreach (Unit unit in units)
            {
                var dist = position.GetDistance(unit.Position);
                if (closest == null || dist < closestDistance)
                {
                    closest = unit;
                    closestDistance = dist;
                }
            }

            return closest;
        }

        /// <summary>
        /// Calculate attack value of a <paramref name="unit"/>.
        /// An attack value is unit's attack power divided by its HP (hull and shileds).
        /// </summary>
        /// <param name="unit">A <see cref="Unit"/> a value of which to calculate.</param>
        /// <returns>An attack value of a <paramref name="unit"/>.</returns>
        public static float GetUnitValue(Unit unit)
        {
            return (unit.WeaponDamage * unit.TotalWeaponsEnergy + unit.WeaponShieldDamage * unit.TotalWeaponsEnergy) /
                   (unit.Hull + unit.Shields.Average());
        }
        
        /// <summary>
        /// A class encapsulating a <see cref="Unit"/> and its value.
        /// </summary>
        public class UnitValue : IDeepCloneable<UnitValue>
        {
            public Unit Unit { get; }
            public float Value { get; }

            public UnitValue(Unit unit, float value)
            {
                Unit = unit;
                Value = value;
            }

            public UnitValue DeepClone()
            {
                return new UnitValue(Unit.DeepClone(), Value);
            }
        }

        /// <summary>
        /// Condition for finding position preferably in weapons' range.
        /// </summary>
        /// <returns></returns>
        public static bool FindPosPrefWepRangeCond(Hex hex, Hex target, Unit unit, GameEnvironment environment)
        {
            int dist = hex.GetDistance(target);
            return dist <= unit.SensorsEnergy;
        }
    }
}
