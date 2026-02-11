using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TicTacToeFancy.Services;

namespace TicTacToeFancy.ViewModels;

public partial class LeaderboardViewModel : ViewModelBase
{
    private readonly DatabaseService _dbService;
    private readonly MainWindowViewModel _mainVm;

    [ObservableProperty]
    private ObservableCollection<PlayerStat> _stats = new();

    [ObservableProperty]
    private bool _isLoading;

    public LeaderboardViewModel(DatabaseService dbService, MainWindowViewModel mainVm)
    {
        _dbService = dbService;
        _mainVm = mainVm;
    }

    /// <summary>
    /// Loads stats asynchronously without blocking the UI thread.
    /// Uses Dispatcher to update the ObservableCollection on UI thread.
    /// </summary>
    public async Task LoadStatsAsync()
    {
        IsLoading = true;

        try
        {
            // Fetch data on background thread
            var data = await _dbService.GetLeaderboardAsync();

            // Update collection on UI thread
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Stats.Clear();
                foreach (var stat in data)
                {
                    Stats.Add(stat);
                }
            });
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void Back()
    {
        _mainVm.ReturnToGame();
    }
}
