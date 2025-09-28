using System.Globalization;
using System.IO;
using System.Security;

namespace P2PLauncher.Services
{
    internal sealed class FeedbackService
    {
        private readonly string logPath;

        public FeedbackService()
        {
            var appDir = AppDomain.CurrentDomain.BaseDirectory ?? ".";
            var logsDir = Path.Combine(appDir, "logs");
            try
            {
                _ = Directory.CreateDirectory(logsDir);
            }
            catch (ArgumentException)
            {
                // best-effort: invalid path
            }
            catch (PathTooLongException)
            {
                // best-effort: path too long
            }
            catch (NotSupportedException)
            {
                // best-effort: path format not supported
            }
            catch (UnauthorizedAccessException)
            {
                // best-effort: no permission to create logs directory
            }
            catch (IOException)
            {
                // best-effort: ignore IO failures creating logs directory
            }
            catch (SecurityException)
            {
                // best-effort: security policy prevents creating directory
            }

            logPath = Path.Combine(logsDir, "diagnostics.log");
        }

        public void LogWarning(string message, string? context = null)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            try
            {
                var line = string.Format(CultureInfo.InvariantCulture, "{0:u} [WARN] {1} {2}", DateTime.UtcNow, message, string.IsNullOrEmpty(context) ? string.Empty : $"[{context}]");
                File.AppendAllText(logPath, line + Environment.NewLine);
            }
            catch (IOException)
            {
                // best-effort logging
            }
            catch (UnauthorizedAccessException)
            {
                // best-effort logging
            }
        }

        public void LogInfo(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            try
            {
                var line = string.Format(CultureInfo.InvariantCulture, "{0:u} [INFO] {1}", DateTime.UtcNow, message);
                File.AppendAllText(logPath, line + Environment.NewLine);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
