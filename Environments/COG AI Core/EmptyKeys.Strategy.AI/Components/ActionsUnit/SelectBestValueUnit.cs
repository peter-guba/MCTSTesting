using System.Linq;
using EmptyKeys.Strategy.AI.Components.Considerations;
using EmptyKeys.Strategy.Units;

namespace EmptyKeys.Strategy.AI.Components.ActionsUnit
{
    public class SelectBestValueUnit : BehaviorComponentBase
    {
        public override BehaviorReturnCode Behave(IBehaviorContext context)
        {
            var unitContext = context as UnitBehaviorContext;
            var targets = unitContext?.Units;
            var unit = unitContext?.Unit as StaticUnit;
            if (targets == null || unit == null)
            {
                returnCode = BehaviorReturnCode.Failure;
                return returnCode;
            }

            BaseUnit bestTarget = null;
            float bestValue = float.MinValue;
            foreach (var target in targets.OfType<StaticUnit>())
            {
                // Simple value fomula - value = power / HP
                float value = 
                    (target.WeaponShieldDamage * target.TotalWeaponsEnergy + target.WeaponDamage * target.TotalWeaponsEnergy) / 
                    (target.Shields.MaximumValue + target.Hull);
                if (value > bestValue)
                {
                    bestValue = value;
                    bestTarget = target;
                }
            }

            unit.Target = bestTarget;

            returnCode = BehaviorReturnCode.Success;
            return returnCode;
        }
    }
}
