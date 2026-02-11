using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace TicTacToeFancy.Models;

public partial class GameCell(int index) : ObservableObject, IDisposable
{
    private bool _disposed;

    public IRelayCommand? MoveCommand { get; set; }

    public void Dispose()
    {
        if (!_disposed)
        {
            _sparkCts?.Cancel();
            _sparkCts?.Dispose();
            _sparkCts = null;
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }

    public int Index { get; } = index;

    // Row/column helpers for staggered animations
    public int Row => Index / 3;
    public int Col => Index % 3;

    [ObservableProperty]
    private string _value = string.Empty;

    [ObservableProperty]
    private bool _isWinner;

    [ObservableProperty]
    private bool _isPlayerX;

    [ObservableProperty]
    private bool _isPlayerO;

    // --- Spark FX ---
    [ObservableProperty]
    private double _sparkOpacity;

    [ObservableProperty]
    private double _sparkScale = 0.5;

    [ObservableProperty]
    private double _aiSparkOpacity;

    [ObservableProperty]
    private double _sparkRingOpacity;

    [ObservableProperty]
    private double _sparkRingScale = 0.7;

    [ObservableProperty]
    private double _sparkRing2Opacity;

    [ObservableProperty]
    private double _sparkRing2Scale = 0.5;

    [ObservableProperty]
    private double _sparkFlashOpacity;

    [ObservableProperty]
    private double _sparkDotDistance = 8;

    [ObservableProperty]
    private double _winnerPulse;

    // --- Intro stagger animation ---
    [ObservableProperty]
    private double _introOpacity;

    [ObservableProperty]
    private double _introScale = 0.6;

    [ObservableProperty]
    private double _introTranslateY = 30;

    // --- Idle ambient glow ---
    [ObservableProperty]
    private double _idleGlow;

    private CancellationTokenSource? _sparkCts;

    private static readonly (double opacity, double scale, double translateY, int delayMs)[] IntroFrames =
    [
        (0.15, 0.65, 26, 22),
        (0.32, 0.72, 20, 22),
        (0.50, 0.80, 14, 24),
        (0.66, 0.87, 9, 24),
        (0.78, 0.93, 5, 26),
        (0.87, 0.97, 2, 26),
        (0.93, 1.00, 0, 28),
        (0.97, 1.02, -1, 28),
        (1.00, 1.04, -2, 26),
        (1.00, 1.02, -1, 24),
        (1.00, 1.00, 0, 22),
    ];

    private static readonly (double mainOpacity, double ringOpacity, double ringScale, double ring2Opacity, double ring2Scale, double flashOpacity, double dotDistance, int delayMs)[] SparkFrames =
    [
        (1.0, 1.0, 0.40, 0.90, 0.30, 1.0, 0, 16),    // Initial flash
        (1.0, 0.95, 0.60, 1.0, 0.55, 0.8, 4, 16),
        (0.95, 0.9, 0.85, 0.9, 0.85, 0.6, 12, 16),
        (0.9, 0.8, 1.15, 0.8, 1.20, 0.4, 25, 20),   // Expansion
        (0.8, 0.65, 1.45, 0.6, 1.60, 0.2, 45, 24),
        (0.6, 0.50, 1.70, 0.4, 1.95, 0.1, 70, 28),
        (0.4, 0.35, 1.90, 0.2, 2.20, 0.05, 95, 32),
        (0.2, 0.20, 2.05, 0.1, 2.35, 0.02, 110, 36),
        (0.1, 0.10, 2.15, 0.05, 2.45, 0.01, 120, 40),
        (0.0, 0.0, 2.20, 0.0, 2.50, 0.0, 130, 20)
    ];

    partial void OnValueChanged(string value)
    {
        IsPlayerX = value == "X";
        IsPlayerO = value == "O";
    }

    public async Task PlayIntroAsync()
    {
        // Stagger delay: top-left first, bottom-right last
        int delay = (Row + Col) * 80 + 60;
        await Task.Delay(delay);

        // Animate in over ~12 frames (~280ms)
        foreach (var (opacity, scale, translateY, delayMs) in IntroFrames)
        {
            IntroOpacity = opacity;
            IntroScale = scale;
            IntroTranslateY = translateY;
            await Task.Delay(delayMs);
        }

        IntroOpacity = 1;
        IntroScale = 1;
        IntroTranslateY = 0;
    }

    public async Task TriggerSparkAsync(bool isAiSpark)
    {
        _sparkCts?.Cancel();
        _sparkCts?.Dispose();
        _sparkCts = new CancellationTokenSource();
        var token = _sparkCts.Token;

        // "Cosmic Energy" shockwave effect
        // High frequency expansion, less "fire" more "plasma"
        try
        {
            foreach (var (mainOpacity, ringOpacity, ringScale, ring2Opacity, ring2Scale, flashOpacity, dotDistance, delayMs) in SparkFrames)
            {
                token.ThrowIfCancellationRequested();
                SparkOpacity = isAiSpark ? 0 : mainOpacity;
                AiSparkOpacity = isAiSpark ? mainOpacity : 0;
                SparkScale = ringScale;
                SparkRingOpacity = isAiSpark ? 0 : ringOpacity;
                SparkRingScale = ringScale;
                SparkRing2Opacity = isAiSpark ? 0 : ring2Opacity;
                SparkRing2Scale = ring2Scale;
                SparkFlashOpacity = flashOpacity;
                SparkDotDistance = dotDistance;

                await Task.Delay(delayMs, token);
            }
        }
        catch (OperationCanceledException)
        {
            // Overlapping spark animations cancel previous runs.
        }
    }
}
