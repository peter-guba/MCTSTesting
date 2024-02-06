using System.Linq;
using EmptyKeys.Strategy.Units;

namespace EmptyKeys.Strategy.AI.Components.ActionsPlayer
{
    public class SetAllAgressive : BehaviorComponentBase
    {
        public override BehaviorReturnCode Behave(IBehaviorContext context)
        {
            var playerContext = context as PlayerBehaviorContext;
            var player = playerContext?.Player;
            if (player?.Units == null)
            {
                returnCode = BehaviorReturnCode.Failure;
                return returnCode;
            }

            foreach (var unit in player.Units.OfType<StaticUnit>())
            {
                unit.Behavior = BehaviorsManager.Instance.GetBehavior("UnitBrainAgressive");
                unit.BehaviorName = "UnitBrainAgressive";
            }

            returnCode = BehaviorReturnCode.Success;
            return returnCode;
        }
    }
}