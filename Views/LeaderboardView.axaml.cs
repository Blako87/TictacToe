using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using TicTacToeFancy.ViewModels;

namespace TicTacToeFancy.Views;

public partial class LeaderboardView : UserControl
{
    public LeaderboardView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
