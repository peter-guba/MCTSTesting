using System.Collections.Generic;

namespace ChessTesting
{
    /// <summary>
    /// A class that stores relevant data about the outcome of a game.
    /// </summary>
    public class GameResult
    {
        /// <summary>
        /// The result of the game (either the winner or, if there is no winner, information about how
        /// the game was terminated.
        /// </summary>
        public GameManager.Result Result { get; set; }

        /// <summary>
        /// The player that won the game or null if there was no winner.
        /// </summary>
        public Player Winner { get; set; }

        /// <summary>
        /// The number of rounds it took to finish the game.
        /// </summary>
        public int RoundCount { get; set; }

        /// <summary>
        /// The time it took the algorithms to pick a move in every round.
        /// </summary>
        public List<double>[] RoundTimes { get; set; }

        /// <summary>
        /// The remaining piece points of the winner (or of the white player, if there was no winner).
        /// The values of the individual pieces can be found in the Evaluation class at the top.
        /// </summary>
        public float RemainingPiecePointsWinner { get; set; }

        /// <summary>
        /// The remaining piece points of the loser (or of the black player, if there was no winner).
        /// The values of the individual pieces can be found in the Evaluation class at the top.
        /// </summary>
        public float RemainingPiecePointsLoser { get; set; }

        public GameResult(GameManager.Result res, Player winner, bool team, int roundCount, List<double>[] roundTimes, Board finalState)
        {
            Result = res;
            Winner = winner;
            RoundCount = roundCount;
            RoundTimes = roundTimes;

            Evaluation eval = new Evaluation();
            RemainingPiecePointsWinner = eval.EvalBoard(finalState, team, res == GameManager.Result.BlackIsMated || res == GameManager.Result.WhiteIsMated, true);
            RemainingPiecePointsLoser = eval.EvalBoard(finalState, !team, res == GameManager.Result.BlackIsMated || res == GameManager.Result.WhiteIsMated, true);
        }

        /// <summary>
        /// Determines whether the game resulted in some player's win.
        /// </summary>
        public bool IsFinished()
        {
            return Result == GameManager.Result.WhiteIsMated || Result == GameManager.Result.BlackIsMated;
        }

        /// <summary>
        /// Fetches the index of the winner.
        ///  0 <=> the given player won
        ///  1 <=> the other player won
        /// -2 <=> there was no winner
        /// </summary>
        /// <param name="search1"></param>
        /// <returns></returns>
        public int GetWinner(ISearch search1)
        {
            if (Winner == null)
            {
                return -2;
            }
            else if (Winner.GetSearch() == search1)
            {
                return 0;
            }
            else
            {
                return 1;
            }
        }
    }
}

