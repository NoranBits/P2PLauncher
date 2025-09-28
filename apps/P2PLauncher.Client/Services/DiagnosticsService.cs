using Serilog;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace P2PLauncher.Client.Services;

public class DiagnosticsService : IDiagnosticsService
{
    private readonly ILogger _logger;

    public DiagnosticsService()
    {
        _logger = Log.ForContext<DiagnosticsService>();
    }

    public async Task<DiagnosticResult> PingAsync(string hostname, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(hostname))
        {
            return new DiagnosticResult(false, "Hostname cannot be empty", TimeSpan.Zero);
        }

        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            _logger.Information("Starting ICMP ping to {Hostname}", hostname);
            
            using var ping = new Ping();
            var reply = await ping.SendPingAsync(hostname, 5000); // 5 second timeout
            
            stopwatch.Stop();
            
            if (reply.Status == IPStatus.Success)
            {
                var message = $"Ping to {hostname} successful: {reply.RoundtripTime}ms";
                _logger.Information("Ping successful: {Hostname}, RTT: {RoundtripTime}ms", hostname, reply.RoundtripTime);
                return new DiagnosticResult(true, message, stopwatch.Elapsed);
            }
            else
            {
                var message = $"Ping to {hostname} failed: {reply.Status}";
                _logger.Warning("Ping failed: {Hostname}, Status: {Status}", hostname, reply.Status);
                return new DiagnosticResult(false, message, stopwatch.Elapsed);
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var message = $"Ping to {hostname} failed: {ex.Message}";
            _logger.Error(ex, "Ping exception: {Hostname}", hostname);
            return new DiagnosticResult(false, message, stopwatch.Elapsed);
        }
    }

    public async Task<DiagnosticResult> TcpConnectAsync(string hostname, int port, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(hostname))
        {
            return new DiagnosticResult(false, "Hostname cannot be empty", TimeSpan.Zero);
        }

        if (port <= 0 || port > 65535)
        {
            return new DiagnosticResult(false, "Port must be between 1 and 65535", TimeSpan.Zero);
        }

        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            _logger.Information("Starting TCP connect to {Hostname}:{Port}", hostname, port);
            
            using var tcpClient = new TcpClient();
            
            // Set timeout using cancellation token
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(10)); // 10 second timeout
            
            await tcpClient.ConnectAsync(hostname, port, timeoutCts.Token);
            
            stopwatch.Stop();
            
            if (tcpClient.Connected)
            {
                var message = $"TCP connection to {hostname}:{port} successful";
                _logger.Information("TCP connect successful: {Hostname}:{Port}", hostname, port);
                return new DiagnosticResult(true, message, stopwatch.Elapsed);
            }
            else
            {
                var message = $"TCP connection to {hostname}:{port} failed: Not connected";
                _logger.Warning("TCP connect failed: {Hostname}:{Port} - Not connected", hostname, port);
                return new DiagnosticResult(false, message, stopwatch.Elapsed);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            var message = $"TCP connection to {hostname}:{port} cancelled";
            _logger.Information("TCP connect cancelled: {Hostname}:{Port}", hostname, port);
            return new DiagnosticResult(false, message, stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var message = $"TCP connection to {hostname}:{port} failed: {ex.Message}";
            _logger.Error(ex, "TCP connect exception: {Hostname}:{Port}", hostname, port);
            return new DiagnosticResult(false, message, stopwatch.Elapsed);
        }
    }
}