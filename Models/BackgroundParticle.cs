using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TicTacToeFancy.Models;

public partial class BackgroundParticle : ObservableObject
{
    public BackgroundParticle(
        double x,
        double y,
        double size,
        bool isAmber,
        double baseOpacity,
        double amplitude,
        double speed,
        double phase,
        double driftX,
        double driftY)
    {
        X = x;
        Y = y;
        Size = size;
        IsAmber = isAmber;
        BaseOpacity = baseOpacity;
        Amplitude = amplitude;
        Speed = speed;
        Phase = phase;
        DriftX = driftX;
        DriftY = driftY;
        Opacity = baseOpacity;
    }

    [ObservableProperty]
    private double _x;

    [ObservableProperty]
    private double _y;

    public double Size { get; }
    public bool IsAmber { get; }
    public double BaseOpacity { get; }
    public double Amplitude { get; }
    public double Speed { get; }
    public double Phase { get; }
    public double DriftX { get; }
    public double DriftY { get; }

    [ObservableProperty]
    private double _opacity;

    public void Update(double time, double width, double height)
    {
        var value = BaseOpacity + Math.Sin(time * Speed + Phase) * Amplitude;
        Opacity = Math.Clamp(value, 0.04, 0.75);

        X += DriftX;
        Y += DriftY;

        if (X > width + Size)
        {
            X = -Size;
        }
        else if (X < -Size)
        {
            X = width + Size;
        }

        if (Y > height + Size)
        {
            Y = -Size;
        }
        else if (Y < -Size)
        {
            Y = height + Size;
        }
    }
}
