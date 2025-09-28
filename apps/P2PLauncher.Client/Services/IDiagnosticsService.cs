namespace P2PLauncher.Client.Services;

public record DiagnosticResult(bool Success, string Message, TimeSpan Duration);

public interface IDiagnosticsService
{
    Task<DiagnosticResult> PingAsync(string hostname, CancellationToken cancellationToken = default);
    Task<DiagnosticResult> TcpConnectAsync(string hostname, int port, CancellationToken cancellationToken = default);
}