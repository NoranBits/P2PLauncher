using Serilog;
using System.Reactive.Subjects;

namespace P2PLauncher.Client.Services;

public class FeedbackService : IFeedbackService, IDisposable
{
    private readonly ILogger _logger;
    private readonly Subject<string> _logLinesSubject;
    private readonly string _logDirectory;
    private readonly SemaphoreSlim _writeSemaphore;

    public FeedbackService()
    {
        _logger = Log.ForContext<FeedbackService>();
        _logLinesSubject = new Subject<string>();
        _logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
        _writeSemaphore = new SemaphoreSlim(1, 1);

        // Ensure logs directory exists
        Directory.CreateDirectory(_logDirectory);
    }

    public IObservable<string> LogLines => _logLinesSubject.AsObservable();

    public async Task AppendLogLineAsync(string message)
    {
        await AppendLogLineAsync(message, CancellationToken.None);
    }

    public async Task AppendLogLineAsync(string message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var logEntry = $"[{timestamp}] {message}";

        try
        {
            await _writeSemaphore.WaitAsync(cancellationToken);

            // Write to diagnostics log file with rotation
            var logFilePath = Path.Combine(_logDirectory, "diagnostics.log");
            await RotateLogFileIfNeededAsync(logFilePath, cancellationToken);
            await File.AppendAllTextAsync(logFilePath, logEntry + Environment.NewLine, cancellationToken);

            // Log via Serilog
            _logger.Information(message);

            // Notify observers
            _logLinesSubject.OnNext(logEntry);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to append log line: {Message}", message);
        }
        finally
        {
            _writeSemaphore.Release();
        }
    }

    private async Task RotateLogFileIfNeededAsync(string logFilePath, CancellationToken cancellationToken)
    {
        if (!File.Exists(logFilePath))
            return;

        var fileInfo = new FileInfo(logFilePath);
        const long maxSizeBytes = 10 * 1024 * 1024; // 10MB

        if (fileInfo.Length > maxSizeBytes)
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            var rotatedPath = Path.Combine(_logDirectory, $"diagnostics-{timestamp}.log");
            
            try
            {
                File.Move(logFilePath, rotatedPath);
                _logger.Information("Rotated log file to {RotatedPath}", rotatedPath);
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Failed to rotate log file");
            }
        }
    }

    public void Dispose()
    {
        _writeSemaphore?.Dispose();
        _logLinesSubject?.Dispose();
    }
}