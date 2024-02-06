using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CMS;
using CMS.Units;
using EmptyKeys.Strategy.Units;
using EmptyKeys.Strategy.Units.Configuration;

namespace EmptyKeys.Strategy.AI.Micro
{
    internal static class UnitFactory
    {
        public static Unit MakeUnit(StaticUnit unit)
        {
            var mu = unit as MoveableUnit;
            if (mu != null)
            {
                return MakeFromMoveable(mu);
            }

            return MakeFromStatic(unit);
        }

        private static Unit MakeFromMoveable(MoveableUnit moveUnit)
        {
            Unit unit = MakeFromStatic(moveUnit);

            unit.AvailEnginesEnergy = moveUnit.AvailEnginesEnergy;
            unit.EnginesEnergyPct = moveUnit.EnginesEnergyPct;
            unit.TotalEnginesEnergy = moveUnit.TotalEnginesEnergy;

            return unit;
        }

        private static Unit MakeFromStatic(StaticUnit sUnit)
        {
            var shieldsStr = new float[sUnit.Shields.Count];
            for (int i = 0; i < sUnit.Shields.Count; i++)
            {
                ShieldConfiguration shieldConfiguration = sUnit.Item.UnitConfig.Shields.FirstOrDefault(s => s.Direction == (ShieldDirection) i);
                if (shieldConfiguration != null)
                {
                    shieldsStr[i] = shieldConfiguration.Strength / 100.0f;
                }
            }

            var unit = new Unit
            {
                AvailEnginesEnergy = 0,
                AvailWeaponsEnergy = sUnit.AvailWeaponsEnergy,
                EnginesEnergyPct = 0,
                GlobalKey = sUnit.GlobalKey,
                Hull = sUnit.Hull,
                HullMax = sUnit.HullMax,
                IsResourcePenaltyActive = sUnit.IsResourcePenaltyActive,
                Position = new Hex(sUnit.Q, sUnit.R),
                ResourcePenaltyModifier = sUnit.Owner.GameSession.EnvironmentConfig.FactoryConfig
                    .ResourcePenaltyModifier,
                SensorsEnergy = sUnit.SensorsEnergy,
                SensorsEnergyPct = sUnit.SensorsEnergyPct,
                Shields = (float[])sUnit.Shields.Values.Clone(),
                ShieldsEnergyPct = sUnit.ShieldsEnergyPct,
                ShieldsRechargeRate = sUnit.ShieldsRechargeRate,
                ShieldsStr = shieldsStr,
                TotalEnergy = sUnit.TotalEnergy,
                TotalEnginesEnergy = 0,
                TotalSensorsEnergy = sUnit.TotalSensorsEnergy,
                TotalWeaponsEnergy = sUnit.TotalWeaponsEnergy,
                WeaponDamage = sUnit.WeaponDamage,
                WeaponShieldDamage = sUnit.WeaponShieldDamage,
                WeaponsEnergyPct = sUnit.WeaponsEnergyPct,
                Direction = sUnit.Direction
            };

            return unit;
        }
    }
}
