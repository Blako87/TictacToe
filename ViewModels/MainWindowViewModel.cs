using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TicTacToeFancy.Models;
using TicTacToeFancy.Services;

namespace TicTacToeFancy.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, IDisposable, IGameNavigator
{
    private const string HumanPlayer = "X";
    private const string AIPlayer = "O";

    private readonly MinimaxAiService _aiMoveService;
    private readonly KiNemotron _kiNemotron;
    private readonly DispatcherTimer _particleTimer;
    private double _particleTime;
    private const double ParticleFieldWidth = 980;
    private const double ParticleFieldHeight = 790;

    [ObservableProperty]
    private string _currentPlayer = HumanPlayer;

    [ObservableProperty]
    private string _statusMessage = "INITIALIZING...";

    [ObservableProperty]
    private int _scoreX;

    [ObservableProperty]
    private int _scoreO;

    [ObservableProperty]
    private bool _isGameActive = true;

    [ObservableProperty]
    private bool _isVsAI = true;

    [ObservableProperty]
    private bool _isAIThinking;

    [ObservableProperty]
    private AiDifficulty _selectedDifficulty = AiDifficulty.Normal;

    // --- Status bar glow ---
    [ObservableProperty]
    private double _statusGlow;

    [ObservableProperty]
    private bool _statusHighlight;

    // --- Scanline sweep ---
    [ObservableProperty]
    private double _scanlineY;

    [ObservableProperty]
    private double _scanlineOpacity = 0.2;

    // --- Turn transition border glow ---
    [ObservableProperty]
    private double _turnGlowOpacity;

    [ObservableProperty]
    private bool _isPlayerXTurn = true;

    [ObservableProperty]
    private string _playerName = "Player 1";

    [ObservableProperty]
    private ViewModelBase _currentView;

    public LeaderboardViewModel LeaderboardVm { get; }

    public ObservableCollection<GameCell> Cells { get; } = [];
    public ObservableCollection<BackgroundParticle> BackgroundParticles { get; } = [];

    private readonly DatabaseService _dbService;

    public bool IsGameActiveAndVisible => IsGameActive && CurrentView == this;

    public bool IsEasySelected => SelectedDifficulty == AiDifficulty.Easy;
    public bool IsNormalSelected => SelectedDifficulty == AiDifficulty.Normal;
    public bool IsHardSelected => SelectedDifficulty == AiDifficulty.Hard;

    public MainWindowViewModel(DatabaseService dbService, MinimaxAiService aiMoveService, KiNemotron kiNemotron, bool startAnimations = true)
    {
        _dbService = dbService;
        _aiMoveService = aiMoveService;
        _kiNemotron = kiNemotron;
        LeaderboardVm = new LeaderboardViewModel(_dbService, this);
        _currentView = this; // Default to game view (self, or we can separate GameViewModel if we want, but keeping it simple for now)

        for (int i = 0; i < 9; i++)
        {
            Cells.Add(new GameCell(i) { MoveCommand = MakeMoveCommand });
        }

        SeedBackgroundParticles();
        _particleTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) };
        _particleTimer.Tick += (_, _) => AnimateFrame();
        if (startAnimations)
        {
            _particleTimer.Start();
            _ = PlayIntroSequenceAsync();
        }
    }

    private async Task PlayIntroSequenceAsync()
    {
        // Fire all cell intros concurrently (each cell has its own stagger delay)
        var introTasks = Cells.Select(c => c.PlayIntroAsync()).ToArray();
        await Task.WhenAll(introTasks);

        // After all cells landed, do a status glow pulse
        StatusMessage = "X starts";
        await PulseStatusGlowAsync();
    }

    private void SeedBackgroundParticles()
    {
        BackgroundParticles.Add(new BackgroundParticle(68, 54, 18, false, 0.15, 0.23, 1.4, 0, 0.22, 0.08));
        BackgroundParticles.Add(new BackgroundParticle(242, 132, 12, true, 0.12, 0.2, 1.05, 1.8, -0.08, 0.19));
        BackgroundParticles.Add(new BackgroundParticle(524, 74, 14, false, 0.14, 0.24, 1.2, 3.1, 0.18, 0.11));
        BackgroundParticles.Add(new BackgroundParticle(736, 178, 10, true, 0.1, 0.2, 0.9, 2.2, -0.11, 0.14));
        BackgroundParticles.Add(new BackgroundParticle(126, 452, 16, false, 0.13, 0.22, 1.1, 4.2, 0.16, -0.09));
        BackgroundParticles.Add(new BackgroundParticle(674, 506, 13, true, 0.11, 0.2, 1.3, 5, -0.19, -0.06));
        BackgroundParticles.Add(new BackgroundParticle(806, 402, 11, false, 0.1, 0.18, 1.15, 0.8, -0.14, 0.12));
        BackgroundParticles.Add(new BackgroundParticle(358, 560, 15, true, 0.12, 0.21, 0.95, 3.8, 0.12, -0.15));
        BackgroundParticles.Add(new BackgroundParticle(918, 76, 9, false, 0.08, 0.16, 1.45, 1.2, -0.26, 0.1));
        BackgroundParticles.Add(new BackgroundParticle(882, 610, 17, true, 0.13, 0.24, 1.18, 4.7, -0.18, -0.11));
        BackgroundParticles.Add(new BackgroundParticle(500, 640, 7, false, 0.07, 0.14, 1.65, 2.7, 0.27, -0.03));
        BackgroundParticles.Add(new BackgroundParticle(56, 666, 21, true, 0.16, 0.25, 0.86, 0.3, 0.12, -0.07));
        BackgroundParticles.Add(new BackgroundParticle(310, 210, 6, false, 0.05, 0.11, 1.9, 1.4, 0.31, 0.08));
        BackgroundParticles.Add(new BackgroundParticle(610, 338, 8, true, 0.06, 0.12, 1.72, 5.7, -0.23, 0.16));
        BackgroundParticles.Add(new BackgroundParticle(784, 302, 20, false, 0.14, 0.22, 0.78, 3.6, -0.09, -0.04));
        BackgroundParticles.Add(new BackgroundParticle(198, 332, 11, true, 0.09, 0.17, 1.35, 2.1, 0.21, 0.09));
    }

    private void AnimateFrame()
    {
        if (CurrentView != this)
        {
            return;
        }

        _particleTime += 0.066;

        // Background particles
        foreach (var particle in BackgroundParticles)
        {
            particle.Update(_particleTime, ParticleFieldWidth, ParticleFieldHeight);
        }

        // Winner pulse
        foreach (var cell in Cells)
        {
            if (cell.IsWinner)
            {
                cell.WinnerPulse = 0.45 + Math.Sin(_particleTime * 2.6 + cell.Index) * 0.25;
            }

            // Idle ambient glow on empty cells
            if (string.IsNullOrEmpty(cell.Value) && IsGameActive)
            {
                cell.IdleGlow = 0.06 + Math.Sin(_particleTime * 1.5 + cell.Index * 0.7) * 0.04;
            }
            else
            {
                cell.IdleGlow = 0;
            }
        }

        // Scanline sweep: moves top to bottom every ~3.5s, then resets
        double scanCycle = _particleTime % 5.5;
        ScanlineY = (scanCycle / 5.5) * 480;
        ScanlineOpacity = 0.08 + Math.Sin(scanCycle * 1.8) * 0.06;

        // Turn glow pulse
        TurnGlowOpacity = 0.25 + Math.Sin(_particleTime * 2.2) * 0.15;

        // Status glow ambient
        if (!StatusHighlight)
        {
            StatusGlow = 0.12 + Math.Sin(_particleTime * 1.8) * 0.08;
        }
    }

    private async Task PulseStatusGlowAsync()
    {
        StatusHighlight = true;

        var frames = new (double glow, int delayMs)[]
        {
            (0.0, 30), (0.3, 30), (0.6, 35), (0.85, 40), (1.0, 50),
            (0.9, 45), (0.75, 45), (0.55, 40), (0.35, 35), (0.18, 30), (0.0, 30)
        };

        foreach (var (glow, delayMs) in frames)
        {
            StatusGlow = glow;
            await Task.Delay(delayMs);
        }

        StatusHighlight = false;
    }

    partial void OnSelectedDifficultyChanged(AiDifficulty value)
    {
        OnPropertyChanged(nameof(IsEasySelected));
        OnPropertyChanged(nameof(IsNormalSelected));
        OnPropertyChanged(nameof(IsHardSelected));
    }

    [RelayCommand]
    private async Task MakeMove(GameCell? cell)
    {
        if (cell is null || !IsGameActive || IsAIThinking || !string.IsNullOrEmpty(cell.Value))
        {
            return;
        }

        await ApplyMoveAsync(cell, CurrentPlayer);
        if (CheckGameEnd())
        {
            return;
        }

        await SwitchPlayerAsync();

        if (IsVsAI && CurrentPlayer == AIPlayer && IsGameActive)
        {
            await MakeAIMoveAsync();
        }
    }

    private async Task MakeAIMoveAsync()
    {
        IsAIThinking = true;
        StatusMessage = "AI is thinking...";

        await Task.Delay(280);

        var boardSnapshot = CreateBoardSnapshot();
        int bestMove = await Task.Run(async () =>
        {
            // Convert the board array to a string representation for the AI
            string boardString = ConvertBoardToString(boardSnapshot);
            var aiResponse = await _kiNemotron.GetAiResponseAsync(boardString);
            // Parse the AI response to get the best move index
            // Assuming the AI response contains the index as an integer
            if (int.TryParse(aiResponse, out int move))
            {
                return move;
            }
            else
            {
                // Fallback to minimax if AI response is invalid
                return _aiMoveService.FindBestMove(boardSnapshot, AIPlayer, HumanPlayer, SelectedDifficulty);
            }
        });


        if (bestMove >= 0 && IsGameActive && string.IsNullOrEmpty(Cells[bestMove].Value))
        {
            await ApplyMoveAsync(Cells[bestMove], AIPlayer);

            if (!CheckGameEnd())
            {
                await SwitchPlayerAsync();
            }
        }

        IsAIThinking = false;
    }

    private string ConvertBoardToString(string[] board)
    {
        // Convert X/O/empty to 1/2/0 format for the AI
        var converted = new string[9];
        for (int i = 0; i < board.Length; i++)
        {
            converted[i] = board[i] == "X" ? "1" : board[i] == "O" ? "2" : "0";
        }
        // Format as [row1], [row2], [row3]
        return $"[{converted[0]}, {converted[1]}, {converted[2]}]\n[{converted[3]}, {converted[4]}, {converted[5]}]\n[{converted[6]}, {converted[7]}, {converted[8]}]";
    }

    private static async Task ApplyMoveAsync(GameCell cell, string player)
    {
        cell.Value = player;
        await cell.TriggerSparkAsync(player == AIPlayer);
    }

    private async Task SwitchPlayerAsync()
    {
        CurrentPlayer = CurrentPlayer == HumanPlayer ? AIPlayer : HumanPlayer;
        IsPlayerXTurn = CurrentPlayer == HumanPlayer;
        StatusMessage = $"{CurrentPlayer} turn";
        await PulseStatusGlowAsync();
    }

    private string[] CreateBoardSnapshot()
    {
        var snapshot = new string[Cells.Count];
        for (int i = 0; i < Cells.Count; i++)
        {
            snapshot[i] = Cells[i].Value;
        }

        return snapshot;
    }

    private bool CheckGameEnd()
    {
        var board = CreateBoardSnapshot();

        if (GameEngine.TryGetWinningLine(board, out var winningIndices))
        {
            IsGameActive = false;

            foreach (var idx in winningIndices)
            {
                Cells[idx].IsWinner = true;
            }

            if (CurrentPlayer == HumanPlayer)
            {
                ScoreX++;
                StatusMessage = IsVsAI ? "VICTORY" : "X WINS";
                if (IsVsAI)
                {
                    _dbService.UpdateStats(PlayerName, "WIN");
                    _dbService.UpdateStats("AI", "LOSS");
                }
            }
            else
            {
                ScoreO++;
                StatusMessage = IsVsAI ? "DEFEATED" : "O WINS";
                if (IsVsAI)
                {
                    _dbService.UpdateStats(PlayerName, "LOSS");
                    _dbService.UpdateStats("AI", "WIN");
                }
            }

            _ = PulseStatusGlowAsync();
            return true;
        }

        if (GameEngine.IsBoardFull(board))
        {
            IsGameActive = false;
            StatusMessage = "DRAW";
            if (IsVsAI)
            {
                _dbService.UpdateStats(PlayerName, "DRAW");
                _dbService.UpdateStats("AI", "DRAW");
            }
            _ = PulseStatusGlowAsync();
            return true;
        }

        return false;
    }

    [RelayCommand]
    private async Task ResetGame()
    {
        foreach (var cell in Cells)
        {
            cell.Value = string.Empty;
            cell.IsWinner = false;
            cell.SparkOpacity = 0;
            cell.AiSparkOpacity = 0;
            cell.SparkScale = 0.5;
            cell.SparkRingOpacity = 0;
            cell.SparkRingScale = 0.7;
            cell.SparkRing2Opacity = 0;
            cell.SparkRing2Scale = 0.5;
            cell.SparkFlashOpacity = 0;
            cell.SparkDotDistance = 8;
            cell.WinnerPulse = 0;
            cell.IntroOpacity = 0;
            cell.IntroScale = 0.6;
            cell.IntroTranslateY = 30;
            cell.IdleGlow = 0;
        }

        CurrentPlayer = HumanPlayer;
        IsPlayerXTurn = true;
        IsGameActive = true;
        IsAIThinking = false;
        StatusMessage = "RESETTING...";

        // Replay staggered intro
        var introTasks = Cells.Select(c => c.PlayIntroAsync()).ToArray();
        await Task.WhenAll(introTasks);

        StatusMessage = "X starts";
        await PulseStatusGlowAsync();
    }

    [RelayCommand]
    private async Task NewGame()
    {
        ScoreX = 0;
        ScoreO = 0;
        await ResetGame();
    }

    [RelayCommand]
    private async Task ToggleGameMode()
    {
        IsVsAI = !IsVsAI;
        await NewGame();
    }

    [RelayCommand]
    private void SelectEasyDifficulty()
    {
        SelectedDifficulty = AiDifficulty.Easy;
    }

    [RelayCommand]
    private void SelectNormalDifficulty()
    {
        SelectedDifficulty = AiDifficulty.Normal;
    }

    [RelayCommand]
    private void SelectHardDifficulty()
    {
        SelectedDifficulty = AiDifficulty.Hard;
    }

    [RelayCommand]
    private async Task ShowLeaderboard()
    {
        _particleTimer.Stop();
        CurrentView = LeaderboardVm;
        await LeaderboardVm.LoadStatsAsync();
    }

    public void ReturnToGame()
    {
        CurrentView = this;
        _particleTimer.Start();
    }

    private bool _disposed;

    public void Dispose()
    {
        if (!_disposed)
        {
            _particleTimer?.Stop();
            foreach (var cell in Cells)
            {
                cell.Dispose();
            }
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
