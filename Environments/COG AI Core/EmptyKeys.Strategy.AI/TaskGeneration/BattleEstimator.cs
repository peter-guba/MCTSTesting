using EmptyKeys.Strategy.Units;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmptyKeys.Strategy.AI.TaskGeneration
{
    class ArmyStats
    {
        public ArmyStats(IEnumerable<StaticUnit> army)
        {
            foreach (var unit in army)
            {
                Hull += unit.Hull;
                Shields += unit.Shields.MaximumValue;
                HullDamage += unit.WeaponDamage * unit.AvailWeaponsEnergy;
                ShieldDamage += unit.WeaponShieldDamage * unit.AvailWeaponsEnergy;
            }
        }

        public float Hull { get; set; }
        public float Shields { get; set; }
        public float HullDamage { get; set; }
        public float ShieldDamage { get; set; }
    }

    static class BattleEstimator
    {
        /// <summary>
        /// Given two army compositions returns whether the first one is more likely to win a direct battle.
        /// </summary>
        /// <param name="alpha">First army</param>
        /// <param name="beta">Second army</param>
        /// <returns>True if <paramref name="alpha"/> wins false otherwise.</returns>
        public static bool Estimate(IEnumerable<StaticUnit> alpha, IEnumerable<StaticUnit> beta)
        {
            var alphaStats = new ArmyStats(alpha);
            var betaStats = new ArmyStats(beta);
            
            float alphaHP = CalculateHP(alphaStats, betaStats);
            float betaHP = CalculateHP(betaStats, alphaStats);

            return alphaHP > betaHP;
        }

        static float CalculateHP(ArmyStats defender, ArmyStats attacker)
        {
            return 
                defender.Shields - attacker.ShieldDamage + 
                defender.Hull - attacker.HullDamage;
        }

        public static float EstimateStrength(IEnumerable<StaticUnit> army)
        {
            return army.Sum(unit => 
                unit.Shields.MaximumValue + 
                unit.Hull + 
                unit.WeaponShieldDamage * unit.TotalWeaponsEnergy + 
                unit.WeaponDamage * unit.TotalWeaponsEnergy
            );
        }
    }
}
