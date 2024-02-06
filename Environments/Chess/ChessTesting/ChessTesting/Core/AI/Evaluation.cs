using System;
using System.Collections;
using System.Collections.Generic;

namespace ChessTesting
{
    public class Evaluation
    {
        // The values of the individual pieces that are used when
        // computing the value of the board.
        public const int pawnValue = 100;
        public const int knightValue = 300;
        public const int bishopValue = 320;
        public const int rookValue = 500;
        public const int queenValue = 900;
        public const int kingValue = 1200;

        /// <summary>
        /// An upper bound on the value of a chess board - correspond to all pieces of one player being present while
        /// there are no pieces belonging to the other player. (This value can be exceeded however, if the pawns are
        /// transformed into other pieces, but given that it is quite rare for this to happen while the player has
        /// enough pieces to exceed this value, I decided to ignore it.)
        /// </summary>
        public const int maxBoardValue = 8 * pawnValue + 2 * knightValue + 2 * bishopValue + 2 * rookValue + queenValue + kingValue;

        const float endgameMaterialStart = rookValue * 2 + bishopValue + knightValue;
        public Board board;

        /// <summary>
        /// Performs static evaluation of the current position.
        /// The position is assumed to be 'quiet', i.e no captures are available that could drastically affect the evaluation.
        /// The score that's returned is given from the perspective of whoever's turn it is to move.
        /// So a positive score means the player who's turn it is to move has an advantage, while a negative score indicates a disadvantage.
        /// </summary>
        public int Evaluate(Board board)
        {
            this.board = board;
            int whiteEval = 0;
            int blackEval = 0;

            int whiteMaterial = CountMaterial(Board.WhiteIndex);
            int blackMaterial = CountMaterial(Board.BlackIndex);

            int whiteMaterialWithoutPawns = whiteMaterial - board.pawns[Board.WhiteIndex].Count * pawnValue;
            int blackMaterialWithoutPawns = blackMaterial - board.pawns[Board.BlackIndex].Count * pawnValue;
            float whiteEndgamePhaseWeight = EndgamePhaseWeight(whiteMaterialWithoutPawns);
            float blackEndgamePhaseWeight = EndgamePhaseWeight(blackMaterialWithoutPawns);

            whiteEval += whiteMaterial;
            blackEval += blackMaterial;
            whiteEval += MopUpEval(Board.WhiteIndex, Board.BlackIndex, whiteMaterial, blackMaterial, blackEndgamePhaseWeight);
            blackEval += MopUpEval(Board.BlackIndex, Board.WhiteIndex, blackMaterial, whiteMaterial, whiteEndgamePhaseWeight);

            whiteEval += EvaluatePieceSquareTables(Board.WhiteIndex, blackEndgamePhaseWeight);
            blackEval += EvaluatePieceSquareTables(Board.BlackIndex, whiteEndgamePhaseWeight);

            int eval = whiteEval - blackEval;

            int perspective = (board.WhiteToMove) ? 1 : -1;
            return eval * perspective;
        }

        /// <summary>
        /// Computes the value of the given board from the perspective of the given player.
        /// </summary>
        /// <param name="b"> The board to evaluate. </param>
        /// <param name="team"> The team from the perspective of which it should be evaluated. </param>
        /// <param name="checkmate"> Determines whether one of the players is in checkmate. </param>
        /// <param name="onlyGivenTeamPoints"> Determines whether only the points of the given team
        /// are supposed to be taken into account. If set to false, the difference between the points
        /// of the given team and the other team is returned. </param>
        /// <returns></returns>
        public float EvalBoard(Board b, bool team, bool checkmate, bool onlyGivenTeamPoints = false)
        {
            this.board = b;
            int result;

            int whiteMaterial = CountMaterial(Board.WhiteIndex);
            int blackMaterial = CountMaterial(Board.BlackIndex);

            if (checkmate)
            {
                if (board.WhiteToMove)
                {
                    blackMaterial += kingValue;
                }
                else
                {
                    whiteMaterial += kingValue;
                }
            }
            else
            {
                whiteMaterial += kingValue;
                blackMaterial += kingValue;
            }

            if (onlyGivenTeamPoints)
            {
                result = team ? whiteMaterial : blackMaterial;
            }
            else
            {
                result = team ? whiteMaterial - blackMaterial : blackMaterial - whiteMaterial;
            }
            return result;
        }

        float EndgamePhaseWeight(int materialCountWithoutPawns)
        {
            const float multiplier = 1 / endgameMaterialStart;
            return 1 - System.Math.Min(1, materialCountWithoutPawns * multiplier);
        }

        int MopUpEval(int friendlyIndex, int opponentIndex, int myMaterial, int opponentMaterial, float endgameWeight)
        {
            int mopUpScore = 0;
            if (myMaterial > opponentMaterial + pawnValue * 2 && endgameWeight > 0)
            {

                int friendlyKingSquare = board.KingSquare[friendlyIndex];
                int opponentKingSquare = board.KingSquare[opponentIndex];
                mopUpScore += PrecomputedMoveData.centreManhattanDistance[opponentKingSquare] * 10;
                // use ortho dst to promote direct opposition
                mopUpScore += (14 - PrecomputedMoveData.NumRookMovesToReachSquare(friendlyKingSquare, opponentKingSquare)) * 4;

                return (int)(mopUpScore * endgameWeight);
            }
            return 0;
        }

        public int CountMaterial(int colourIndex)
        {
            int material = 0;
            material += board.pawns[colourIndex].Count * pawnValue;
            material += board.knights[colourIndex].Count * knightValue;
            material += board.bishops[colourIndex].Count * bishopValue;
            material += board.rooks[colourIndex].Count * rookValue;
            material += board.queens[colourIndex].Count * queenValue;

            return material;
        }

        /// <summary>
        /// Gets the heuristic value of the given piece. 
        /// </summary>
        /// <param name="piece"></param>
        /// <returns></returns>
        public int GetPieceValue(int piece)
        {
            int type = Piece.PieceType(piece);

            switch(type)
            {
                case 0: return 0;
                case 2: return pawnValue;
                case 3: return knightValue;
                case 5: return bishopValue;
                case 6: return rookValue;
                case 7: return queenValue;
                default: throw new ArgumentException("Something's wrong, I can feel it.");
            }
        }

        int EvaluatePieceSquareTables(int colourIndex, float endgamePhaseWeight)
        {
            int value = 0;
            bool isWhite = colourIndex == Board.WhiteIndex;
            value += EvaluatePieceSquareTable(PieceSquareTable.pawns, board.pawns[colourIndex], isWhite);
            value += EvaluatePieceSquareTable(PieceSquareTable.rooks, board.rooks[colourIndex], isWhite);
            value += EvaluatePieceSquareTable(PieceSquareTable.knights, board.knights[colourIndex], isWhite);
            value += EvaluatePieceSquareTable(PieceSquareTable.bishops, board.bishops[colourIndex], isWhite);
            value += EvaluatePieceSquareTable(PieceSquareTable.queens, board.queens[colourIndex], isWhite);
            int kingEarlyPhase = PieceSquareTable.Read(PieceSquareTable.kingMiddle, board.KingSquare[colourIndex], isWhite);
            value += (int)(kingEarlyPhase * (1 - endgamePhaseWeight));
            //value += PieceSquareTable.Read (PieceSquareTable.kingMiddle, board.KingSquare[colourIndex], isWhite);

            return value;
        }

        static int EvaluatePieceSquareTable(int[] table, PieceList pieceList, bool isWhite)
        {
            int value = 0;
            for (int i = 0; i < pieceList.Count; i++)
            {
                value += PieceSquareTable.Read(table, pieceList[i], isWhite);
            }
            return value;
        }
    }
}