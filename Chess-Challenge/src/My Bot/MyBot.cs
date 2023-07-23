// AapBot_V1 +59=92-3

using ChessChallenge.API;
using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;

public class MyBot : IChessBot {
  private static Dictionary<PieceType, int> pieceValues = new Dictionary<PieceType, int>() {
    {PieceType.Pawn, 100},
    {PieceType.Knight, 305},
    {PieceType.Bishop, 333},
    {PieceType.Rook, 563},
    {PieceType.Queen, 950}
  };

  private static float GetDistanceBetweenSquares(Square a, Square b) {
    return (float)Math.Sqrt(Math.Pow(b.File - a.File, 2) + Math.Pow(b.Rank - a.Rank, 2));
  }

  private static float GetSideScore(Board board, bool white) {
    int pieceScore = 0;
    List<float> distances = new List<float>();
    Square opponentKing = board.GetPieceList(PieceType.King, !white)[0].Square;

    foreach (PieceType type in pieceValues.Keys) {
      PieceList list = board.GetPieceList(type, white);
      int pieceValue = pieceValues[type];
      pieceScore += pieceValue * list.Count;

      for (int i = 0; i < list.Count; i += 1) {
        Piece piece = list[i];
        int distanceToFront = white ? piece.Square.Rank : 7 - piece.Square.Rank;
        float distanceToKing = GetDistanceBetweenSquares(piece.Square, opponentKing);
        float maxDistanceToKing = 9.899f;

        int distancesToFrontScore = distanceToFront / 7;
        float distanceToKingScore =  1 - (distanceToKing / maxDistanceToKing);
        distances.Add(distancesToFrontScore + distanceToKingScore);
      }
    }

    float positionScore = distances.Count == 0 ? 0 : distances.Average() * 50;
    return pieceScore + positionScore;
  }

  private static float GetBoardScore(Board board, bool white) {
    return GetSideScore(board, white) - GetSideScore(board, !white);
  }

  private static Move? GetBestMove(Board board) {
    Move[] moves = board.GetLegalMoves();

    if (moves.Length == 0) {
      return null;
    }

    float bestScore = float.MinValue;
    Move bestMove = moves[0];
    bool white = board.IsWhiteToMove;

    foreach (Move move in moves) {
      board.MakeMove(move);
      float score = GetBoardScore(board, white);
      board.UndoMove(move);

      if (score > bestScore) {
        bestScore = score;
        bestMove = move;
      }
    }

    return bestMove;
  }

  public static Move GetBestMoveWithForesight(Board board) {
    Move[] moves = board.GetLegalMoves();
    Move?[] rebukes = new Move?[moves.Length];
    for (int i = 0; i < moves.Length; i += 1) {
      Move move = moves[i];
      board.MakeMove(move);
      Move? opponentsBestMove = GetBestMove(board);
      board.UndoMove(move);

      rebukes[i] = opponentsBestMove;
    }

    float bestScore = float.MinValue;
    Move bestMove = moves[0];

    for (int i = 0; i < moves.Length; i += 1) {
      Move move = moves[i];
      Move? opponentsBestMove = rebukes[i];

      // if opponent can't rebuke, it's a winning move I think
      if (opponentsBestMove == null) {
        return move;
      }

      board.MakeMove(move);
      board.MakeMove((Move)opponentsBestMove);
      float score = GetBoardScore(board, board.IsWhiteToMove);
      board.UndoMove((Move)opponentsBestMove);
      board.UndoMove(move);

      if (score > bestScore) {
        bestScore = score;
        bestMove = move;
      }
    }

    return bestMove;
  }

  public Move Think(Board board, Timer timer) {
    bool white = board.IsWhiteToMove;
    // get current best move for opponent
    bool skipped = board.TrySkipTurn();
    if (skipped) {
      Move? opponentsBestMove = GetBestMove(board);
      board.UndoSkipTurn();

      if (opponentsBestMove != null) {
        bool opponentWouldCapture = ((Move)opponentsBestMove).CapturePieceType != PieceType.None;
        if (opponentWouldCapture) {
          Piece underThreat = board.GetPiece(((Move)opponentsBestMove).TargetSquare);
          // get all squares that the piece can move to
          Move[] moves = board.GetLegalMoves();

          float bestScore = float.MinValue;
          Move? bestMove = null;

          foreach (Move move in moves) {
            if (move.StartSquare == underThreat.Square) {
              board.MakeMove(move);
              float score = GetBoardScore(board, white);
              board.UndoMove(move);

              if (score > bestScore) {
                bestScore = score;
                bestMove = move;
              }
            }
          }

          if (bestMove != null) {
            return (Move)bestMove;
          }
        }
      }
    }

    return GetBestMoveWithForesight(board);
  }
}