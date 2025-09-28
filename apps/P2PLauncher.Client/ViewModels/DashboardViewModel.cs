using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using P2PLauncher.Client.Services;
using Serilog;
using System.Collections.ObjectModel;
using System.Windows;

namespace P2PLauncher.Client.ViewModels;

public partial class DashboardViewModel : ObservableObject, IDisposable
{
    private readonly IFeedbackService _feedbackService;
    private readonly ILogger _logger;
    private readonly IDisposable _logSubscription;
    private CancellationTokenSource? _operationCts;

    [ObservableProperty]
    private string _connectionState = "Disconnected";

    [ObservableProperty]
    private int _peerCount = 0;

    [ObservableProperty]
    private ObservableCollection<string> _recentLogLines = new();

    public DashboardViewModel(IFeedbackService feedbackService)
    {
        _feedbackService = feedbackService;
        _logger = Log.ForContext<DashboardViewModel>();

        // Subscribe to log lines and ensure UI thread marshalling
        _logSubscription = _feedbackService.LogLines.Subscribe(logLine =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                RecentLogLines.Add(logLine);
                
                // Keep only the last 100 log lines
                while (RecentLogLines.Count > 100)
                {
                    RecentLogLines.RemoveAt(0);
                }
            });
        });

        // Add initial log message
        _ = _feedbackService.AppendLogLineAsync("Dashboard initialized");
    }

    [RelayCommand(CanExecute = nameof(CanStartHost))]
    private async Task StartHostAsync()
    {
        _operationCts?.Cancel();
        _operationCts = new CancellationTokenSource();

        try
        {
            _logger.Information("Starting host operation");
            ConnectionState = "Starting Host...";
            
            await _feedbackService.AppendLogLineAsync("Starting P2P host...", _operationCts.Token);
            
            // Simulate host startup
            await Task.Delay(2000, _operationCts.Token);
            
            if (!_operationCts.Token.IsCancellationRequested)
            {
                ConnectionState = "Host Active";
                PeerCount = 0;
                await _feedbackService.AppendLogLineAsync("P2P host started successfully");
                _logger.Information("Host operation completed successfully");
            }
        }
        catch (OperationCanceledException)
        {
            ConnectionState = "Disconnected";
            await _feedbackService.AppendLogLineAsync("Host startup cancelled");
            _logger.Information("Host operation cancelled");
        }
        catch (Exception ex)
        {
            ConnectionState = "Error";
            await _feedbackService.AppendLogLineAsync($"Host startup failed: {ex.Message}");
            _logger.Error(ex, "Host operation failed");
        }
    }

    [RelayCommand(CanExecute = nameof(CanConnect))]
    private async Task ConnectAsync()
    {
        _operationCts?.Cancel();
        _operationCts = new CancellationTokenSource();

        try
        {
            _logger.Information("Starting connect operation");
            ConnectionState = "Connecting...";
            
            await _feedbackService.AppendLogLineAsync("Connecting to P2P network...", _operationCts.Token);
            
            // Simulate connection process
            await Task.Delay(3000, _operationCts.Token);
            
            if (!_operationCts.Token.IsCancellationRequested)
            {
                ConnectionState = "Connected";
                PeerCount = Random.Shared.Next(1, 10);
                await _feedbackService.AppendLogLineAsync($"Connected to P2P network. Found {PeerCount} peers.");
                _logger.Information("Connect operation completed successfully");
            }
        }
        catch (OperationCanceledException)
        {
            ConnectionState = "Disconnected";
            await _feedbackService.AppendLogLineAsync("Connection cancelled");
            _logger.Information("Connect operation cancelled");
        }
        catch (Exception ex)
        {
            ConnectionState = "Error";
            await _feedbackService.AppendLogLineAsync($"Connection failed: {ex.Message}");
            _logger.Error(ex, "Connect operation failed");
        }
    }

    [RelayCommand(CanExecute = nameof(CanDisconnect))]
    private async Task DisconnectAsync()
    {
        _operationCts?.Cancel();
        _operationCts = new CancellationTokenSource();

        try
        {
            _logger.Information("Starting disconnect operation");
            ConnectionState = "Disconnecting...";
            
            await _feedbackService.AppendLogLineAsync("Disconnecting from P2P network...", _operationCts.Token);
            
            // Simulate disconnection process
            await Task.Delay(1000, _operationCts.Token);
            
            if (!_operationCts.Token.IsCancellationRequested)
            {
                ConnectionState = "Disconnected";
                PeerCount = 0;
                await _feedbackService.AppendLogLineAsync("Disconnected from P2P network");
                _logger.Information("Disconnect operation completed successfully");
            }
        }
        catch (OperationCanceledException)
        {
            await _feedbackService.AppendLogLineAsync("Disconnection cancelled");
            _logger.Information("Disconnect operation cancelled");
        }
        catch (Exception ex)
        {
            ConnectionState = "Error";
            await _feedbackService.AppendLogLineAsync($"Disconnection failed: {ex.Message}");
            _logger.Error(ex, "Disconnect operation failed");
        }
    }

    private bool CanStartHost() => ConnectionState is "Disconnected" or "Error";
    private bool CanConnect() => ConnectionState is "Disconnected" or "Error";
    private bool CanDisconnect() => ConnectionState is "Connected" or "Host Active";

    public void Dispose()
    {
        _operationCts?.Cancel();
        _operationCts?.Dispose();
        _logSubscription?.Dispose();
    }
}