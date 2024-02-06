namespace ChessTesting
{
    /// <summary>
    /// An intnerface that represents an algorithm that searches for good moves.
    /// </summary>
    public interface ISearch
    {
        /// <summary>
        /// Searches for a move and returns it.
        /// </summary>
        Move StartSearch();

        /// <summary>
        /// Sets the chess board configuration that the algorithm currently sees.
        /// </summary>
        void SetBoard(Board board);

        /// <summary>
        /// Sets the name of the battle that is currently running.
        /// </summary>
        void SetBattleName(string name);

        /// <summary>
        /// Sets the random string that is added to the ends of files that contain time
        /// and depth logs.
        /// </summary>
        void SetRndBattleString(string str);
    }
}
