using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System;
using System.IO;
using System.Net.NetworkInformation;

namespace ChessTesting
{
	public class GameManager
	{
        private static Random r = new Random();

		/// <summary>
		/// A stream writer used when logging checkmate test results.
		/// </summary>
		private static StreamWriter sw;

        /// <summary>
        /// An enum that lists the different possible results of a chess game, including the different
        /// terminating conditions.
        /// </summary>
        public enum Result { Playing, WhiteIsMated, BlackIsMated, Stalemate, Repetition, FiftyMoveRule, InsufficientMaterial, TooManyMoves }

		/// <summary>
		/// The default settings of an alpha-beta player.
		/// </summary>
		public AlphaBetaSettings alphaBetaSettings;

		/// <summary>
		/// The default settings of an MCTS player.
		/// </summary>
		public MCTSSettings mctsSettings;

		/// <summary>
		/// The result of the currently played game.
		/// </summary>
		Result gameResult;

		Player whitePlayer;
		Player blackPlayer;
		Player playerToMove;

		/// <summary>
		/// List of moves performed in the current game thus far.
		/// </summary>
		List<Move> gameMoves;

		/// <summary>
		/// The current game board.
		/// </summary>
		public Board board { get; private set; }

        /// <summary>
        /// Duplicate version of the board variable used for AI search.
        /// </summary>
        Board searchBoard;

		public GameManager() {
			gameMoves = new List<Move> ();
			board = new Board ();
			searchBoard = new Board ();

			alphaBetaSettings = new AlphaBetaSettings();
			alphaBetaSettings.diagnostics = new SearchDiagnostics ();
			alphaBetaSettings.depth = 3;
			alphaBetaSettings.useIterativeDeepening = true;
			alphaBetaSettings.useTranspositionTable = true;
			alphaBetaSettings.useFixedDepthSearch = true;
			alphaBetaSettings.clearTTEachMove = true;

			mctsSettings = new MCTSSettings();
			mctsSettings.diagnostics = new SearchDiagnostics();
		}

		/// <summary>
		/// Starts a new game with AI players that use the given search algorithms.
		/// </summary>
		public void NewGame(ISearch whitePlayerSearch, ISearch blackPlayerSearch)
        {
			gameMoves.Clear();
            board.LoadStartPosition();
            searchBoard.LoadStartPosition();

			whitePlayer = new AIPlayer(whitePlayerSearch);
			blackPlayer = new AIPlayer(blackPlayerSearch);

			gameResult = Result.Playing;
		}

		/// <summary>
		/// Performs a simulation of a chess game and returns its result. The game needs to
		/// first be set up using the NewGame method.
		/// </summary>
		/// <param name="whitePlayerIndex"> The index of the white player (used when storing 
		/// round time). </param>
		public GameResult Simulate(int whitePlayerIndex, int maxRounds)
        {
			gameResult = GetGameState();
			int counter = 0;
			List<double>[] times = new List<double>[] { new List<double>(), new List<double>() };
			var sw = new Stopwatch();
			int playerIndex = whitePlayerIndex;
			whitePlayer.SetBoard(searchBoard);
			blackPlayer.SetBoard(searchBoard);

			// While the game isn't finished.
			while (gameResult == Result.Playing && counter < maxRounds)
            {
				++counter;
				playerToMove = (board.WhiteToMove) ? whitePlayer : blackPlayer;

				// Have the player whose move it is pick a move and measure how long it takes.
				sw.Restart();
				playerToMove.PickAMove();
				sw.Stop();

				PerformMove(playerToMove.GetMove());
				gameResult = GetGameState();

				times[playerIndex].Add(sw.Elapsed.TotalMilliseconds);
				playerIndex = 1 - playerIndex;
			}

			// Return the relevant data using the GameResult class.
			if (gameResult == Result.WhiteIsMated)
			{
				return new GameResult(gameResult, blackPlayer, false, counter, times, board);
			}
			else if (gameResult == Result.BlackIsMated)
			{
				return new GameResult(gameResult, whitePlayer, true, counter, times, board);
			}
			else
			{
				return new GameResult(gameResult, null, true, counter, times, board);
			}
		}

		/// <summary>
		/// Determines and returns the current state of the game.
		/// </summary>
		private Result GetGameState () {
			MoveGenerator moveGenerator = new MoveGenerator ();
			var moves = moveGenerator.GenerateMoves (board);

			// Look for mate/stalemate
			if (moves.Count == 0) {
				if (moveGenerator.InCheck ()) {
					return (board.WhiteToMove) ? Result.WhiteIsMated : Result.BlackIsMated;
				}
				return Result.Stalemate;
			}

			// Fifty move rule
			if (board.fiftyMoveCounter >= 100) {
				return Result.FiftyMoveRule;
			}

			// Threefold repetition
			int repCount = board.RepetitionPositionHistory.Count ((x => x == board.ZobristKey));
			if (repCount == 3) {
				return Result.Repetition;
			}

			// Look for insufficient material (not all cases implemented yet)
			int numPawns = board.pawns[Board.WhiteIndex].Count + board.pawns[Board.BlackIndex].Count;
			int numRooks = board.rooks[Board.WhiteIndex].Count + board.rooks[Board.BlackIndex].Count;
			int numQueens = board.queens[Board.WhiteIndex].Count + board.queens[Board.BlackIndex].Count;
			int numKnights = board.knights[Board.WhiteIndex].Count + board.knights[Board.BlackIndex].Count;
			int numBishops = board.bishops[Board.WhiteIndex].Count + board.bishops[Board.BlackIndex].Count;

			if (numPawns + numRooks + numQueens == 0) {
				if (numKnights == 1 || numBishops == 1) {
					return Result.InsufficientMaterial;
				}
			}

			return Result.Playing;
		}

		/// <summary>
		/// Performs the given move on the current chess board.
		/// </summary>
		void PerformMove(Move move)
        {
			if (gameResult == Result.Playing)
			{
				board.MakeMove(move);
				searchBoard.MakeMove(move);

				gameMoves.Add(move);
			}
		}

		/// <summary>
		/// Runs checkmate tests. These are tests which check how soon the MCTS variants can identify the
		/// best move in on a chess board where checkmate is possible in a given number of moves.
		/// </summary>
		public void RunCheckmateTests(int numOfRepeats, int testIndex, BasicMCTS mcts)
		{
			// Set up the alpha-beta player that works as the opponent in these tests.
            alphaBetaSettings.endlessSearchMode = false;
            alphaBetaSettings.promotionsToSearch = MoveGenerator.PromotionMode.All;
            alphaBetaSettings.depth = 4;
            alphaBetaSettings.useIterativeDeepening = true;
            alphaBetaSettings.useTranspositionTable = true;
            alphaBetaSettings.useFixedDepthSearch = true;
            alphaBetaSettings.clearTTEachMove = false;

			string outputDir = "./checkmate_tests/";
            
            if (!Directory.Exists(outputDir + mcts.name))
                Directory.CreateDirectory(outputDir + mcts.name);

			sw = new StreamWriter("./checkmate_tests/" + mcts.name + '/' + testIndex + '_' + DateTime.Now.ToString("yyyy_MM_dd-HH_mm_ss") + '_' + r.Next(100000, 1000000) + ".csv");

            // Run the given test.
            TestSettings.Setting s = TestSettings.tests[testIndex];

			sw.Write("" + s.numOfMoves + '.' + (testIndex % TestSettings.numOfTestsPerMoveCount + 1) + ';');

			// Repeat the test numOfRepeats times.
			for (int i = 0; i < numOfRepeats; ++i)
			{
				Console.WriteLine("Test started");
				Console.WriteLine($"algorithm: {mcts}");
				Console.WriteLine($"setting: {s.fen}");
				Console.WriteLine($"number of moves:{s.numOfMoves}");
				Console.WriteLine();

				// Set up a new game.
				gameMoves.Clear();
				board.LoadPosition(s.fen);
				searchBoard.LoadPosition(s.fen);
				mcts.lookForBestMove = true;
				mcts.SetBestMoveOptions(s.correctMoves);

				if (s.team)
				{
					whitePlayer = new AIPlayer(mcts);
					blackPlayer = new AIPlayer(new AlphaBetaSearch(searchBoard, alphaBetaSettings));
				}
				else
				{
					whitePlayer = new AIPlayer(new AlphaBetaSearch(searchBoard, alphaBetaSettings));
					blackPlayer = new AIPlayer(mcts);
				}

				gameResult = GetGameState();
				whitePlayer.SetBoard(searchBoard);
				blackPlayer.SetBoard(searchBoard);

				// Have the first player pick a move.
				playerToMove = (board.WhiteToMove) ? whitePlayer : blackPlayer;
				playerToMove.PickAMove();

				// If the best move was correctly identified 100 times in a row, the variant
				// succeeded in identifying it and the number of playouts it took to do so
				// is added to the numberOfMoves dictionary.
				if (mcts.GetBestMoveCounter() == 100)
				{
					sw.Write(" 100: " + (mcts.GetPlayoutCounter() - mcts.GetBestMoveCounter() + 1) + ";");
				}
				// Otherwise, -1 is added to signify that the variant didn't succeed.
				else
				{
					sw.Write(" -1;");
				}
			}

			sw.WriteLine();
			sw.Flush();
        }

		public static void LogCheckmateTests(BasicMCTS currentAlgorithm)
		{
            sw.Write(" " + currentAlgorithm.GetBestMoveCounter() + ": " + (currentAlgorithm.GetPlayoutCounter() - currentAlgorithm.GetBestMoveCounter() + 1) + ",");
        }
	}
}