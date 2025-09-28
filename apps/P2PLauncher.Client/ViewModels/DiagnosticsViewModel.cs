using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using P2PLauncher.Client.Services;
using Serilog;

namespace P2PLauncher.Client.ViewModels;

public partial class DiagnosticsViewModel : ObservableObject, IDisposable
{
    private readonly IDiagnosticsService _diagnosticsService;
    private readonly IFeedbackService _feedbackService;
    private readonly ILogger _logger;
    private CancellationTokenSource? _operationCts;

    [ObservableProperty]
    private string _pingHostname = "8.8.8.8";

    [ObservableProperty]
    private string _pingResult = "Ready to ping";

    [ObservableProperty]
    private bool _isPinging = false;

    [ObservableProperty]
    private string _tcpHostname = "google.com";

    [ObservableProperty]
    private string _tcpPort = "80";

    [ObservableProperty]
    private string _tcpResult = "Ready to test connection";

    [ObservableProperty]
    private bool _isTesting = false;

    public DiagnosticsViewModel(IDiagnosticsService diagnosticsService, IFeedbackService feedbackService)
    {
        _diagnosticsService = diagnosticsService;
        _feedbackService = feedbackService;
        _logger = Log.ForContext<DiagnosticsViewModel>();
    }

    [RelayCommand(CanExecute = nameof(CanRunPing))]
    private async Task RunPingAsync()
    {
        if (string.IsNullOrWhiteSpace(PingHostname))
        {
            PingResult = "Please enter a hostname to ping";
            return;
        }

        _operationCts?.Cancel();
        _operationCts = new CancellationTokenSource();

        try
        {
            IsPinging = true;
            PingResult = $"Pinging {PingHostname}...";
            
            _logger.Information("Starting ping diagnostic for {Hostname}", PingHostname);
            await _feedbackService.AppendLogLineAsync($"Starting ICMP ping to {PingHostname}");

            var result = await _diagnosticsService.PingAsync(PingHostname, _operationCts.Token);
            
            PingResult = $"{result.Message} (took {result.Duration.TotalMilliseconds:F0}ms)";
            
            var logMessage = result.Success 
                ? $"Ping successful: {result.Message}"
                : $"Ping failed: {result.Message}";
            
            await _feedbackService.AppendLogLineAsync(logMessage);
            _logger.Information("Ping diagnostic completed: {Success}, {Message}", result.Success, result.Message);
        }
        catch (OperationCanceledException)
        {
            PingResult = "Ping cancelled";
            await _feedbackService.AppendLogLineAsync($"Ping to {PingHostname} was cancelled");
            _logger.Information("Ping diagnostic cancelled");
        }
        catch (Exception ex)
        {
            PingResult = $"Ping failed: {ex.Message}";
            await _feedbackService.AppendLogLineAsync($"Ping error: {ex.Message}");
            _logger.Error(ex, "Ping diagnostic failed");
        }
        finally
        {
            IsPinging = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanRunTcpConnect))]
    private async Task RunTcpConnectAsync()
    {
        if (string.IsNullOrWhiteSpace(TcpHostname))
        {
            TcpResult = "Please enter a hostname to test";
            return;
        }

        if (!int.TryParse(TcpPort, out var port) || port <= 0 || port > 65535)
        {
            TcpResult = "Please enter a valid port (1-65535)";
            return;
        }

        _operationCts?.Cancel();
        _operationCts = new CancellationTokenSource();

        try
        {
            IsTesting = true;
            TcpResult = $"Testing connection to {TcpHostname}:{port}...";
            
            _logger.Information("Starting TCP connect diagnostic for {Hostname}:{Port}", TcpHostname, port);
            await _feedbackService.AppendLogLineAsync($"Starting TCP connection test to {TcpHostname}:{port}");

            var result = await _diagnosticsService.TcpConnectAsync(TcpHostname, port, _operationCts.Token);
            
            TcpResult = $"{result.Message} (took {result.Duration.TotalMilliseconds:F0}ms)";
            
            var logMessage = result.Success 
                ? $"TCP connect successful: {result.Message}"
                : $"TCP connect failed: {result.Message}";
            
            await _feedbackService.AppendLogLineAsync(logMessage);
            _logger.Information("TCP connect diagnostic completed: {Success}, {Message}", result.Success, result.Message);
        }
        catch (OperationCanceledException)
        {
            TcpResult = "Connection test cancelled";
            await _feedbackService.AppendLogLineAsync($"TCP connection test to {TcpHostname}:{TcpPort} was cancelled");
            _logger.Information("TCP connect diagnostic cancelled");
        }
        catch (Exception ex)
        {
            TcpResult = $"Connection test failed: {ex.Message}";
            await _feedbackService.AppendLogLineAsync($"TCP connect error: {ex.Message}");
            _logger.Error(ex, "TCP connect diagnostic failed");
        }
        finally
        {
            IsTesting = false;
        }
    }

    private bool CanRunPing() => !IsPinging;
    private bool CanRunTcpConnect() => !IsTesting;

    public void Dispose()
    {
        _operationCts?.Cancel();
        _operationCts?.Dispose();
    }
}