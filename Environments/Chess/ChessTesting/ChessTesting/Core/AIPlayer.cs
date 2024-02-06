using System.Threading;

namespace ChessTesting
{
	/// <summary>
	/// A class that represents a player controlled by an algorithm.
	/// </summary>
	public class AIPlayer : Player {
		/// <summary>
		/// The search algorithm that this AI player uses to pick moves.
		/// </summary>
		ISearch search;

		/// <summary>
		/// The last move that was picked by this player.
		/// </summary>
		Move move;

		public AIPlayer (ISearch search) {
			this.search = search;
		}

		/// <summary>
		/// Sets the chess board that the search algorithm uses.
		/// </summary>
		public override void SetBoard(Board board)
        {
			search.SetBoard(board);
        }

		/// <summary>
		/// Gets the search algorithm used by this AI player.
		/// </summary>
        public override ISearch GetSearch()
        {
			return search;
        }

		/// <summary>
		/// Picks a move to play.
		/// </summary>
        public override void PickAMove () {
			StartSearch();
		}

		/// <summary>
		/// Fetches the last picked move.
		/// </summary>
		public override Move GetMove()
		{
			return move;
		}

		/// <summary>
		/// Starts searching for a move to play.
		/// </summary>
		void StartSearch () {
			move = search.StartSearch ();
		}
	}
}