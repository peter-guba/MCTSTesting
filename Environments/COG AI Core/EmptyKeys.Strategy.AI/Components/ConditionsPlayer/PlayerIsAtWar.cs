﻿using System.Linq;
using EmptyKeys.Strategy.Core;
using EmptyKeys.Strategy.Diplomacy;

namespace EmptyKeys.Strategy.AI.Components.ConditionsPlayer
{
    /// <summary>
    /// Implements player condition for behavior.
    /// </summary>
    /// <seealso cref="EmptyKeys.Strategy.AI.Components.BehaviorComponentBase" />
    public class PlayerIsAtWar : BehaviorComponentBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PlayerIsAtWar"/> class.
        /// </summary>
        public PlayerIsAtWar()
            : base()
        {
        }

        /// <summary>
        /// Executes behavior with given context
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        public override BehaviorReturnCode Behave(IBehaviorContext context)
        {
            PlayerBehaviorContext playerContext = context as PlayerBehaviorContext;
            if (playerContext == null)
            {
                returnCode = BehaviorReturnCode.Failure;
                return returnCode;
            }

            PlayerRelationValue existingRelation = playerContext.Player.RelationsValues.FirstOrDefault(r => r.IsAtWar);            
            if (existingRelation != null)
            {
                Player relationPlayer = existingRelation.Player;
                if (relationPlayer == null)
                {
                    relationPlayer = playerContext.Player.GameSession.Players.FirstOrDefault(p => p.Index == existingRelation.PlayerIndex);
                    existingRelation.Player = relationPlayer;
                }

                if (relationPlayer != null && !relationPlayer.IsEliminated)
                {
                    returnCode = BehaviorReturnCode.Success;
                    return returnCode;
                }
            }

            returnCode = BehaviorReturnCode.Failure;
            return returnCode;
        }
    }
}
