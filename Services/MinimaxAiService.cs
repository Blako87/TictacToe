using System;
using System.Collections.Generic;

namespace TicTacToeFancy.Services;

public class MinimaxAiService
{
    public int FindBestMove(string[] board, string aiPlayer, string humanPlayer, AiDifficulty difficulty)
    {
        var availableMoves = GetAvailableMoves(board);
        if (availableMoves.Count == 0)
        {
            return -1;
        }

        if (difficulty == AiDifficulty.Easy)
        {
            return availableMoves[Random.Shared.Next(availableMoves.Count)];
        }

        int bestScore = int.MinValue;
        int bestMove = -1;
        int secondBestScore = int.MinValue;
        int secondBestMove = -1;

        for (int i = 0; i < board.Length; i++)
        {
            if (!string.IsNullOrEmpty(board[i]))
            {
                continue;
            }

            board[i] = aiPlayer;
            int score = Minimax(board, false, 0, aiPlayer, humanPlayer);
            board[i] = string.Empty;

            if (score > bestScore)
            {
                secondBestScore = bestScore;
                secondBestMove = bestMove;
                bestScore = score;
                bestMove = i;
            }
            else if (score > secondBestScore)
            {
                secondBestScore = score;
                secondBestMove = i;
            }
        }

        if (difficulty == AiDifficulty.Normal)
        {
            bool playBestMove = Random.Shared.NextDouble() <= 0.65;
            if (!playBestMove && secondBestMove != -1)
            {
                return Random.Shared.Next(2) == 0 ? bestMove : secondBestMove;
            }
        }

        return bestMove;
    }

    private static List<int> GetAvailableMoves(string[] board)
    {
        var moves = new List<int>();
        for (int i = 0; i < board.Length; i++)
        {
            if (string.IsNullOrEmpty(board[i]))
            {
                moves.Add(i);
            }
        }

        return moves;
    }

    private static int Minimax(string[] board, bool isMaximizing, int depth, string aiPlayer, string humanPlayer)
    {
        if (GameEngine.HasWinner(board, aiPlayer))
        {
            return 10 - depth;
        }

        if (GameEngine.HasWinner(board, humanPlayer))
        {
            return depth - 10;
        }

        if (GameEngine.IsBoardFull(board))
        {
            return 0;
        }

        if (isMaximizing)
        {
            int bestScore = int.MinValue;
            for (int i = 0; i < board.Length; i++)
            {
                if (!string.IsNullOrEmpty(board[i]))
                {
                    continue;
                }

                board[i] = aiPlayer;
                int score = Minimax(board, false, depth + 1, aiPlayer, humanPlayer);
                board[i] = string.Empty;
                bestScore = Math.Max(score, bestScore);
            }

            return bestScore;
        }

        int minScore = int.MaxValue;
        for (int i = 0; i < board.Length; i++)
        {
            if (!string.IsNullOrEmpty(board[i]))
            {
                continue;
            }

            board[i] = humanPlayer;
            int score = Minimax(board, true, depth + 1, aiPlayer, humanPlayer);
            board[i] = string.Empty;
            minScore = Math.Min(score, minScore);
        }

        return minScore;
    }
}
