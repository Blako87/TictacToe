using System;

namespace TicTacToeFancy.Services;

public static class GameEngine
{
    public static readonly int[][] WinningLines =
    {
        new[] { 0, 1, 2 }, new[] { 3, 4, 5 }, new[] { 6, 7, 8 },
        new[] { 0, 3, 6 }, new[] { 1, 4, 7 }, new[] { 2, 5, 8 },
        new[] { 0, 4, 8 }, new[] { 2, 4, 6 }
    };

    public static bool TryGetWinningLine(string[] board, out int[] winningIndices)
    {
        foreach (var line in WinningLines)
        {
            if (!string.IsNullOrEmpty(board[line[0]]) &&
                board[line[0]] == board[line[1]] &&
                board[line[1]] == board[line[2]])
            {
                winningIndices = line;
                return true;
            }
        }

        winningIndices = Array.Empty<int>();
        return false;
    }

    public static bool HasWinner(string[] board, string player)
    {
        foreach (var line in WinningLines)
        {
            if (board[line[0]] == player &&
                board[line[1]] == player &&
                board[line[2]] == player)
            {
                return true;
            }
        }

        return false;
    }

    public static bool IsBoardFull(string[] board)
    {
        foreach (var cell in board)
        {
            if (string.IsNullOrEmpty(cell))
            {
                return false;
            }
        }

        return true;
    }
}
