using System.Collections;
using System.Collections.Generic;

namespace ChessTesting
{
	public static class BitBoardUtility {
		public static bool ContainsSquare (ulong bitboard, int square) {
			return ((bitboard >> square) & 1) != 0;
		}
	}
}