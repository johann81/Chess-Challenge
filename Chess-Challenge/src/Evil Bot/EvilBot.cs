using ChessChallenge.API;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ChessChallenge.Example
{
    // A simple bot that can spot mate in one, and always captures the most valuable piece it can.
    // Plays randomly otherwise.
    public class EvilBot : IChessBot
    {
        // Piece values: null, pawn, knight, bishop, rook, queen, king
        private readonly int[] pieceBaseValues = { 0, 150, 250, 250, 500, 900, 100000 };
        private readonly int[] pieceMobilityValues = { 0, 0, 15, 13, 10, 7, 0 };

        

        public Move Think(Board board, Timer timer)
        {
            Move[] allMoves = board.GetLegalMoves();

            IEnumerable<Move> orderedMoves = OrderMoves(board, allMoves, board.IsWhiteToMove);


            // Initialize variables for best move and evaluation value
            Move bestMove = orderedMoves.First(); // initializes with the first ordered move (will be updated later)
            int bestValue = int.MinValue;

            int maxDepth = 3;
            // Checks if the player initially is White

            foreach (var move in orderedMoves)
            {

                if (MoveIsCheckmate(board, move))
                {
                    bestMove = move;
                    break;
                }
                // Make the move
                board.MakeMove(move);

                // Call the minimax function to get the value of the move
                int moveValue = Minimax(board, maxDepth, int.MinValue, int.MaxValue, false);

                board.UndoMove(move);

                if (moveValue > bestValue)
                {
                    bestValue = moveValue;
                    bestMove = move;
                }
                if(timer.MillisecondsElapsedThisTurn > 1500)
                {
                    Console.WriteLine("Terminated Early");
                    return bestMove;

                }
            }

            return bestMove;
        }

        private IEnumerable<Move> OrderMoves(Board board, Move[] moves, bool isMaximizingPlayer)
        {
            List<Move> orderedMoves = new List<Move>();
            List<Move> Checks = new List<Move>();
            List<Move> Captures = new List<Move>();
            List<Move> Attacks = new List<Move>();
            List<Move> Rest = new List<Move>();
            foreach (Move move in moves)
            {

                board.MakeMove(move);
                if (MoveIsCheck(board))
                {
                    Checks.Add(move);
                }
                else if (move.IsCapture)
                {
                    Captures.Add(move);
                }
                else if (isAttack(board, moves))
                {
                    Attacks.Add(move);
                }
                else
                {
                    Rest.Add(move);
                }

                board.UndoMove(move);

            }
            //adds them all
            orderedMoves.AddRange(Checks); orderedMoves.AddRange(Captures); orderedMoves.AddRange(Attacks); orderedMoves.AddRange(Rest);

            return orderedMoves;
        }
        private bool MoveIsCheck(Board board)
        {
            bool isCheck = false;
            if(board.TrySkipTurn() == false) { isCheck = true; }
            board.UndoSkipTurn();
            return isCheck;
        }
        private bool isAttack(Board board, Move[] moves)
        {
            board.ForceSkipTurn();
            bool isAttack = false;
            foreach (Move move in moves)
            {
                if (move.IsCastles)
                {
                    isAttack = true;
                }
            }
            board.UndoSkipTurn();
            return isAttack;

        }
        // Test if this move gives checkmate
        private bool MoveIsCheckmate(Board board, Move move)
        {
            // Apply the move to the board
            board.MakeMove(move);

            // Check if the opponent is in checkmate after the move
            bool isMate = board.IsInCheckmate();

            // Undo the move
            board.UndoMove(move);

            return isMate;
        }

        // Minimax function with alpha-beta pruning
        private int Minimax(Board board, int depth, int alpha, int beta, bool isMaximizingPlayer)
        {

            if (depth == 0 || board.IsDraw() || board.IsInCheckmate())
            {
                int evaluation = Evaluate(board);


                return evaluation;
            }

            Move[] allMoves = board.GetLegalMoves();

            if (isMaximizingPlayer)
            {
                int maxEval = int.MinValue;
                foreach (Move move in allMoves)
                {
                    board.MakeMove(move);
                    int eval = Minimax(board, depth - 1, alpha, beta, false);
                    board.UndoMove(move);

                    maxEval = Math.Max(maxEval, eval);
                    alpha = Math.Max(alpha, eval);

                    if (beta <= alpha)
                        break;
                }


                return maxEval;
            }
            else
            {
                int minEval = int.MaxValue;
                foreach (Move move in allMoves)
                {
                    board.MakeMove(move);
                    int eval = Minimax(board, depth - 1, alpha, beta, true);
                    board.UndoMove(move);

                    minEval = Math.Min(minEval, eval);
                    beta = Math.Min(beta, eval);

                    if (beta <= alpha)
                        break;
                }


                return minEval;
            }
        }

        private int Evaluate(Board board)
        {
            int evaluation = 0;

            PieceList[] allPieceLists = board.GetAllPieceLists();
            Move[] allMoves = board.GetLegalMoves();

            foreach (PieceList pieceList in allPieceLists)
            {
                foreach (Piece piece in pieceList)
                {
                    int pieceValue = pieceBaseValues[(int)piece.PieceType];
                    evaluation += piece.IsWhite ? pieceValue : -pieceValue;

                    // Calculate piece mobility and add it to the evaluation
                    int mobilityValue = pieceMobilityValues[(int)piece.PieceType];
                    int pieceMobility = GetPieceMobility(piece, allMoves);
                    evaluation += piece.IsWhite ? (pieceMobility * mobilityValue) : (-pieceMobility * mobilityValue);
                }
            }

            return board.IsWhiteToMove ? evaluation : -evaluation;
        }

        private int GetPieceMobility(Piece piece, Move[] allMoves)
        {
            int mobility = 0;
            Square pieceSquare = piece.Square;

            foreach (Move move in allMoves)
            {
                if (move.StartSquare == pieceSquare)
                {
                    mobility++;
                }
            }

            return mobility;
        }

    }
}