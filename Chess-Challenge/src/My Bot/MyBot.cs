using ChessChallenge.API;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;

namespace ChessChallenge.Example
{
    // A simple bot that can spot mate in one, and always captures the most valuable piece it can.
    // Plays randomly otherwise.
    public class MyBot : IChessBot
    {
        // Piece values: null, pawn, knight, bishop, rook, queen, king
        private readonly int[] pieceBaseValues = { 0, 150, 250, 250, 500, 900, 100000 };
        private readonly int[] pieceMobilityValues = { 0, 0, 15, 13, 10, 6, 0 };

        // Transposition table to store previously evaluated positions
        private readonly Dictionary<ulong, TranspositionEntry> transpositionTable = new Dictionary<ulong, TranspositionEntry>();

        public Move Think(Board board, Timer timer)
        {
            Move[] allMoves = board.GetLegalMoves();
            // Initialize variables for best move and evaluation value
            Move bestMove = allMoves[0]; // initializes with the first move (will be updated later)
            int bestValue = int.MinValue;

            int maxDepth = 3;


            foreach (Move move in allMoves)
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

                //terminate search if time exceeds 2 seconds
                if(timer.MillisecondsElapsedThisTurn > 1500)
                {
                    return bestMove;
                }
            }

            return bestMove;
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
            // Check if the current position is already in the transposition table
            ulong hash = board.ZobristKey;
            if (transpositionTable.TryGetValue(hash, out var entry))
            {
                if (entry.Depth >= depth)
                {
                    if (entry.NodeType == NodeType.Exact)
                    {
                        return entry.Value;
                    }
                    if (entry.NodeType == NodeType.LowerBound)
                    {
                        alpha = Math.Max(alpha, entry.Value);
                    }
                    else if (entry.NodeType == NodeType.UpperBound)
                    {
                        beta = Math.Min(beta, entry.Value);
                    }

                    if (alpha >= beta)
                    {
                        return entry.Value;
                    }
                }
            }

            if (depth == 0 || board.IsDraw() || board.IsInCheckmate())
            {
                int evaluation = Evaluate(board);

                // Store the evaluation in the transposition table
                TranspositionEntry newEntry = new TranspositionEntry(evaluation, NodeType.Exact, depth);
                transpositionTable[hash] = newEntry;

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

                // Store the evaluation in the transposition table
                NodeType nodeType = (maxEval <= alpha) ? NodeType.UpperBound : NodeType.Exact;
                TranspositionEntry newEntry = new TranspositionEntry(maxEval, nodeType, depth);
                transpositionTable[hash] = newEntry;

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

                // Store the evaluation in the transposition table
                NodeType nodeType = (minEval >= beta) ? NodeType.LowerBound : NodeType.Exact;
                TranspositionEntry newEntry = new TranspositionEntry(minEval, nodeType, depth);
                transpositionTable[hash] = newEntry;

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

        private class TranspositionEntry
        {
            public int Value { get; }
            public NodeType NodeType { get; }
            public int Depth { get; }

            public TranspositionEntry(int value, NodeType nodeType, int depth)
            {
                Value = value;
                NodeType = nodeType;
                Depth = depth;
            }
        }

        private enum NodeType
        {
            Exact,
            LowerBound,
            UpperBound
        }
    }
}