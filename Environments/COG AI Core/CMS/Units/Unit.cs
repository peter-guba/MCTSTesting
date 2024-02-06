using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using CMS.Micro.Scripts;

namespace CMS.Units
{
    /// <summary>
    /// A simplification of the Unit from the real game.
    /// Corresponds to a MoveableUnit from the game.
    /// If we want to use it as a StaticUnit some fields may be left to their default values.
    /// E.g. Unit with TotalEnginesEnergy set to 0 will be unable to move.
    /// </summary>
    [Serializable]
    [XmlRoot]
    public class Unit : IDeepCloneable<Unit>
    {
        [XmlIgnore]
        public int GlobalKey { get; set; }

        [XmlIgnore]
        public Hex Position { get; set; }

        private const int SHIELD_COUNT = 6;

        // Base unit
        public float TotalEnergy { get; set; }
        public float Hull { get; set; }
        public float HullMax { get; set; }
        public bool IsResourcePenaltyActive { get; set; }
        public int Direction { get; set; }

        // Static unit
        public float ShieldsEnergyPct { get; set; }
        public float ShieldsRechargeRate { get; set; }
        public float WeaponsEnergyPct { get; set; }
        public float WeaponDamage { get; set; }
        public float TotalWeaponsEnergy { get; set; }
        public float AvailWeaponsEnergy { get; set; }
        public float SensorsEnergyPct { get; set; }
        public float TotalSensorsEnergy { get; set; }
        public float SensorsEnergy { get; set; }
        public float WeaponShieldDamage { get; set; }
        public float[] Shields { get; set; }
        public float[] ShieldsStr { get; set; }

        // Moveable unit
        public float EnginesEnergyPct { get; set; }
        public float AvailEnginesEnergy { get; set; }
        public float TotalEnginesEnergy { get; set; }

        public float TotalShieldsEnergy => (float)Math.Round(ShieldsEnergyPct / 100.0 * TotalEnergy, 1);

        // Owner
        public float ResourcePenaltyModifier { get; set; }

        [XmlIgnore]
        public IScript Script { get; set; }

        public bool CanMove => AvailEnginesEnergy > 0;

        public bool CanAttack => AvailWeaponsEnergy > 0;

        public List<Hex> GetWeaponTargets()
        {
            var targets = new List<Hex>();
            short range = (short)SensorsEnergy;

            for (int q = -range + Position.Q; q <= range + Position.Q; q++)
            {
                for (int r = -range + Position.R; r <= range + Position.R; r++)
                {
                    targets.Add(new Hex((short)q, (short)r));
                }
            }

            return targets;
        }

        public void PrepareForNextTurn()
        {
            float num1 = TotalShieldsEnergy * (ShieldsRechargeRate / 100f);
            float[] newShieldEnergy = new float[SHIELD_COUNT];
            for (int i = 0; i < Shields.Length; i++)
            {

                float maxShieldStr = TotalShieldsEnergy * (ShieldsStr[i] / 100f);
                newShieldEnergy[i] = Math.Min(Shields[i] + num1, maxShieldStr);
            }
            UpdateAttributes(newShieldEnergy);
        }

        private void UpdateAttributes(float[] newShieldEnergy)
        {
            float resourcePenaltyMod = IsResourcePenaltyActive ? ResourcePenaltyModifier : 1f;
            for (int index = 0; index < Shields.Length; ++index)
            {
                float num3 = (float)Math.Round(newShieldEnergy[index] * (double)resourcePenaltyMod, 2);
                Shields[index] = num3;
            }
            AvailWeaponsEnergy = (float)Math.Round(TotalWeaponsEnergy * (double)resourcePenaltyMod, 1);
            SensorsEnergy = (float)Math.Round(TotalSensorsEnergy * (double)resourcePenaltyMod, 1);

            // Moveable unit
            AvailEnginesEnergy = TotalEnginesEnergy;
        }

        public Unit DeepClone()
        {
            return new Unit
            {
                GlobalKey = GlobalKey,
                Position = Position,
                TotalEnergy = TotalEnergy,
                Hull = Hull,
                HullMax = HullMax,
                IsResourcePenaltyActive = IsResourcePenaltyActive,
                Direction = Direction,
                ShieldsEnergyPct = ShieldsEnergyPct,
                ShieldsRechargeRate = ShieldsRechargeRate,
                WeaponsEnergyPct = WeaponsEnergyPct,
                WeaponDamage = WeaponDamage,
                TotalWeaponsEnergy = TotalWeaponsEnergy,
                AvailWeaponsEnergy = AvailWeaponsEnergy,
                SensorsEnergyPct = SensorsEnergyPct,
                TotalSensorsEnergy = TotalSensorsEnergy,
                SensorsEnergy = SensorsEnergy,
                WeaponShieldDamage = WeaponShieldDamage,
                Shields = (float[])Shields.Clone(),
                ShieldsStr = (float[])ShieldsStr.Clone(),
                EnginesEnergyPct =  EnginesEnergyPct,
                AvailEnginesEnergy = AvailEnginesEnergy,
                TotalEnginesEnergy = TotalEnginesEnergy,
                ResourcePenaltyModifier = ResourcePenaltyModifier,
                Script = Script
            };
        }

        protected bool IsSimilar(Unit other)
        {
            //return Direction == other.Direction;
            return
                GlobalKey == other.GlobalKey &&
                Position.Equals(other.Position) &&
                TotalEnergy.Equals(other.TotalEnergy) &&
                Hull.Equals(other.Hull) &&
                HullMax.Equals(other.HullMax) &&
                //IsResourcePenaltyActive == other.IsResourcePenaltyActive &&
                Direction == other.Direction &&
                ShieldsEnergyPct.Equals(other.ShieldsEnergyPct) &&
                ShieldsRechargeRate.Equals(other.ShieldsRechargeRate) &&
                WeaponsEnergyPct.Equals(other.WeaponsEnergyPct) &&
                WeaponDamage.Equals(other.WeaponDamage) &&
                TotalWeaponsEnergy.Equals(other.TotalWeaponsEnergy) &&
                AvailWeaponsEnergy.Equals(other.AvailWeaponsEnergy) &&
                SensorsEnergyPct.Equals(other.SensorsEnergyPct) &&
                TotalSensorsEnergy.Equals(other.TotalSensorsEnergy) &&
                SensorsEnergy.Equals(other.SensorsEnergy) &&
                WeaponShieldDamage.Equals(other.WeaponShieldDamage) &&
                Shields.SequenceEqual(other.Shields) &&
                ShieldsStr.SequenceEqual(other.ShieldsStr) &&
                EnginesEnergyPct.Equals(other.EnginesEnergyPct) &&
                AvailEnginesEnergy.Equals(other.AvailEnginesEnergy) &&
                TotalEnginesEnergy.Equals(other.TotalEnginesEnergy);
                //ResourcePenaltyModifier.Equals(other.ResourcePenaltyModifier);
        }

        protected bool Equals(Unit other)
        {
            //return IsSimilar(other);
            return
                GlobalKey == other.GlobalKey &&
                Position.Equals(other.Position) &&
                TotalEnergy.Equals(other.TotalEnergy) &&
                Hull.Equals(other.Hull) &&
                HullMax.Equals(other.HullMax) &&
                //IsResourcePenaltyActive == other.IsResourcePenaltyActive &&
                Direction == other.Direction &&
                ShieldsEnergyPct.Equals(other.ShieldsEnergyPct) &&
                ShieldsRechargeRate.Equals(other.ShieldsRechargeRate) &&
                WeaponsEnergyPct.Equals(other.WeaponsEnergyPct) &&
                WeaponDamage.Equals(other.WeaponDamage) &&
                TotalWeaponsEnergy.Equals(other.TotalWeaponsEnergy) &&
                AvailWeaponsEnergy.Equals(other.AvailWeaponsEnergy) &&
                SensorsEnergyPct.Equals(other.SensorsEnergyPct) &&
                TotalSensorsEnergy.Equals(other.TotalSensorsEnergy) &&
                SensorsEnergy.Equals(other.SensorsEnergy) &&
                WeaponShieldDamage.Equals(other.WeaponShieldDamage) &&
                Shields.SequenceEqual(other.Shields) &&
                ShieldsStr.SequenceEqual(other.ShieldsStr) &&
                EnginesEnergyPct.Equals(other.EnginesEnergyPct) &&
                AvailEnginesEnergy.Equals(other.AvailEnginesEnergy) &&
                TotalEnginesEnergy.Equals(other.TotalEnginesEnergy);
                //ResourcePenaltyModifier.Equals(other.ResourcePenaltyModifier);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Unit) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = GlobalKey;
                hashCode = (hashCode * 397) ^ Position.GetHashCode();
                hashCode = (hashCode * 397) ^ TotalEnergy.GetHashCode();
                hashCode = (hashCode * 397) ^ Hull.GetHashCode();
                hashCode = (hashCode * 397) ^ HullMax.GetHashCode();
                hashCode = (hashCode * 397) ^ IsResourcePenaltyActive.GetHashCode();
                hashCode = (hashCode * 397) ^ Direction;
                hashCode = (hashCode * 397) ^ ShieldsEnergyPct.GetHashCode();
                hashCode = (hashCode * 397) ^ ShieldsRechargeRate.GetHashCode();
                hashCode = (hashCode * 397) ^ WeaponsEnergyPct.GetHashCode();
                hashCode = (hashCode * 397) ^ WeaponDamage.GetHashCode();
                hashCode = (hashCode * 397) ^ TotalWeaponsEnergy.GetHashCode();
                hashCode = (hashCode * 397) ^ AvailWeaponsEnergy.GetHashCode();
                hashCode = (hashCode * 397) ^ SensorsEnergyPct.GetHashCode();
                hashCode = (hashCode * 397) ^ TotalSensorsEnergy.GetHashCode();
                hashCode = (hashCode * 397) ^ SensorsEnergy.GetHashCode();
                hashCode = (hashCode * 397) ^ WeaponShieldDamage.GetHashCode();
                hashCode = (hashCode * 397) ^ (Shields != null ? Shields.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (ShieldsStr != null ? ShieldsStr.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ EnginesEnergyPct.GetHashCode();
                hashCode = (hashCode * 397) ^ AvailEnginesEnergy.GetHashCode();
                hashCode = (hashCode * 397) ^ TotalEnginesEnergy.GetHashCode();
                hashCode = (hashCode * 397) ^ ResourcePenaltyModifier.GetHashCode();
                hashCode = (hashCode * 397) ^ (Script != null ? Script.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}
