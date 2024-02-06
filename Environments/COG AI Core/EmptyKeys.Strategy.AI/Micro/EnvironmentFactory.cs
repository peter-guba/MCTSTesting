using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CMS;
using CMS.Units;
using EmptyKeys.Strategy.Core;
using EmptyKeys.Strategy.Environment;
using EmptyKeys.Strategy.Units;

namespace EmptyKeys.Strategy.AI.Micro
{
    internal static class EnvironmentFactory
    {
        public static EnvironmentHexMap MakeEnvironment(BaseEnvironment environment)
        {
            var envMap = new EnvironmentHexMap();

            foreach (HexElement ele in environment.EnvironmentMap.Values)
            {
                if (ele is Star)
                {
                    // Star + 1 hex around are impassable
                    var hex = new Hex(ele.Q, ele.R);
                    envMap.Add(hex, HexType.Impassable);
                    foreach (Hex dir in Constants.HexDirections)
                    {
                        envMap.Add(hex + dir, HexType.Impassable);
                    }
                }
                else if (ele is Asteroid)
                {
                    envMap.Add(new Hex(ele.Q, ele.R), HexType.DoubleCost);
                }
            }

            return envMap;
        }

        public static GameState MakeGameState(
            BaseEnvironment unitEnvi, 
            List<MoveableUnit> moveableUnits,
            PlayerBehaviorContext playerContext)
        {
            var enemyUnits = unitEnvi.UnitsMap.Values.OfType<StaticUnit>().Where(u => u.Owner != playerContext.Player);

            var p1Units = new HexMap<Unit>();
            foreach (MoveableUnit u in moveableUnits)
            {
                p1Units[new Hex(u.Q, u.R)] = UnitFactory.MakeUnit(u);
            }
            var p2Units = new HexMap<Unit>();
            foreach (StaticUnit enemyUnit in enemyUnits)
            {
                p2Units[new Hex(enemyUnit.Q, enemyUnit.R)] = UnitFactory.MakeUnit(enemyUnit);
            }
            var units = new[] { p1Units, p2Units };
            return new GameState(units, 0);
        }
    }
}
