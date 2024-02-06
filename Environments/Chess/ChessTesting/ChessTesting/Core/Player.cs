namespace ChessTesting
{
	/// <summary>
	/// An abstract class that represents a player. The reason for its existence is that, in
	/// the original project, there were two types of players - AI and human. Since the human
	/// player was unnecessary for this project, it was removed and thus this class is now
	/// redundant and could be incorporated into the AIPlayer class, if I cared enough to
	/// do so.
	/// </summary>
	public abstract class Player {

		/// <summary>
		/// Makes the player pick a move.
		/// </summary>
		public abstract void PickAMove ();

		/// <summary>
		/// Fetches the last picked move.
		/// </summary>
		public abstract Move GetMove();

		/// <summary>
		/// Sets the current state of the chess board that the player sees.
		/// </summary>
		public abstract void SetBoard(Board board);

		/// <summary>
		/// Gets the search algorithm used by the player.
		/// </summary>
		public abstract ISearch GetSearch();
	}
}