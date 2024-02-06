using System;
using System.Diagnostics;
using CMS.Units;
using CMS.Utility;

namespace CMS.Actions
{
    public class AttackAction : ActionCms
    {
        public Hex Target { get; }

        public AttackAction(Unit u, Hex target)
            : base(u)
        {
            Target = target;
        }

        public override string ToString()
        {
            return $"Attack action from: {Source.ToString()} to: {Target.ToString()}";
        }

        public override void Execute(GameEnvironment environment)
        {
            Unit attackUnit = environment.GameState.GetUnitAt(Source);
            Unit target = environment.GameState.GetUnitAt(Target);

            Debug.Assert(attackUnit != null, "Unit not found!");
            Debug.Assert(target != null);

            attackUnit.Direction = attackUnit.Position.GetDirection(target.Position);
            int index = (Hex.GetReverseDirection(attackUnit.Direction) - target.Direction + 6) % 6;

            while (true)
            {
                if (!attackUnit.CanAttack)
                {
                    break;
                }

                attackUnit.AvailWeaponsEnergy = (float) Math.Round(attackUnit.AvailWeaponsEnergy - 1.0, 1);
                if (attackUnit.AvailWeaponsEnergy < 0.0)
                {
                    attackUnit.AvailWeaponsEnergy = 0.0f;
                }

                if (target.Shields[index] > 0.0f)
                {
                    float shield = target.Shields[index];
                    target.Shields[index] = (float) Math.Round((double) shield - attackUnit.WeaponShieldDamage, 2);

                    if (target.Shields[index] < 0.0f)
                    {
                        target.Hull = (float) Math.Round((double) target.Hull + target.Shields[index], 1);
                        target.Shields[index] = 0.0f;
                    }
                }
                else
                {
                    target.Hull = (float) Math.Round((double) target.Hull - attackUnit.WeaponDamage, 1);
                }

                if (target.Hull <= 0.0f)
                {
                    target.Hull = 0.0f;
                    environment.GameState.KillUnitAt(target.Position);
                    break;
                }
            }

            Logger.Log($"INFO: Simulated hull for {target.Position} decreased to: {target.Hull}");
        }
    }
}
