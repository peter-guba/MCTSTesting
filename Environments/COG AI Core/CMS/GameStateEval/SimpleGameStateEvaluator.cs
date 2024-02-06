using CMS.Micro.Scripts;
using CMS.Units;

namespace CMS.GameStateEval
{
    /// <inheritdoc />
    internal class SimpleGameStateEvaluator : IGameStateEvaluator
    {
        /// <inheritdoc />
        /// <summary>
        /// Retuns a sum of values of units in given game state using <see cref="MicroHelpers.GetUnitValue"/>.
        /// </summary>
        public double EvaluateGameState(GameState state, int player)
        {
            var result = 0.0;

            foreach (Unit u in state.Units[player].Values)
            {
                result += MicroHelpers.GetUnitValue(u);
            }

            foreach (Unit u in state.Units[GameState.Opo(player)].Values)
            {
                result -= MicroHelpers.GetUnitValue(u);
            }

            return result;
        }
    }
}
