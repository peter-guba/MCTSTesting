namespace CMS.GameStateEval
{
    /// <summary>
    /// Serves for evaluating value of game states.
    /// </summary>
    public interface IGameStateEvaluator
    {
        /// <summary>
        /// Heuristic function which evaluates value of given <paramref name="state"/> from view of given <paramref name="player"/>.
        /// </summary>
        double EvaluateGameState(GameState state, int player);
    }
}
