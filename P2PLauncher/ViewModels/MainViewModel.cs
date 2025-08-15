using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace P2PLauncher.ViewModels;

/// <summary>
/// Main application view-model (MVVM Toolkit). Provides basic bindings and commands.
/// </summary>
internal partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private string host = string.Empty;

    [ObservableProperty]
    private int port = 12000;

    [ObservableProperty]
    private string status = "Idle";

    [ObservableProperty]
    private bool debugEnabled;

    public ObservableCollection<string> RecentHosts { get; } = [];

    public MainViewModel()
    {
        // Seed with a couple of recent entries (replace with persisted data source later)
        RecentHosts.Add("9.0.0.1");
        RecentHosts.Add("9.0.0.2");
    }

    [RelayCommand]
    private void Connect()
    {
        // TODO: Integrate with client connection service and persist successful connections
        Status = $"Connecting to {Host}:{Port}...";
    }

    [RelayCommand]
    private void Stop()
    {
        // TODO: Request FreeLAN stop/cleanup on client side
        Status = "Stopped";
    }
}
