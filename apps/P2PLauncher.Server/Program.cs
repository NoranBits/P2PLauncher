using System.CommandLine;
using System.Globalization;
using P2PLauncher.Exceptions;
using P2PLauncher.Model;
using P2PLauncher.Services;
using Serilog;
using Serilog.Events;
using System.Text.Json;
using System.Text.Json.Serialization;
using P2PLauncher.Server;

JsonSerializerOptions s_jsonOptions = new()
{
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
};

AppDomain.CurrentDomain.UnhandledException += (s, e) => Log.Fatal(e.ExceptionObject as Exception, "UnhandledException");
TaskScheduler.UnobservedTaskException += (s, e) => { if (e?.Exception != null) { Log.Error(e.Exception, "UnobservedTaskException"); } e?.SetObserved(); };

Option<string> passwordOption = new(name: "--password", description: "Passphrase", getDefaultValue: () => string.Empty);
Option<bool> relayOption = new(name: "--relay", description: "Enable relay mode", getDefaultValue: () => false);
Option<bool> debugOption = new(name: "--debug", description: "Show freelan console window", getDefaultValue: () => false);
Option<string> logLevelOption = new(name: "--log-level", description: "Log level (Verbose|Debug|Information|Warning|Error|Fatal)", getDefaultValue: () => "Information");

Command hostCmd = new("host", "Start FreeLAN in host mode")
            {
                passwordOption, relayOption, debugOption, logLevelOption
            };
hostCmd.SetHandler((pwd, relay, debug, logLevel) =>
{
    ConfigureLogging(logLevel);
    try
    {
        Log.Information("Starting HOST (relay={Relay}, debug={Debug})", relay, debug);
        WindowsServices windowsServices = new();
        WinDialogService dialog = new();
        WinFileService files = new();
        FreeLanDetectionService detect = new(files, dialog);
        FreeLanService freelan = new(windowsServices, detect, dialog);
        PeerTracker tracker = new();

        freelan.OutputReceived += line => { Log.Debug("freelan: {Line}", line); tracker.ProcessLine(line); };
        freelan.ServiceStarted += () => Log.Information("freelan started");
        freelan.ServiceStopped += () => Log.Information("freelan stopped");

        if (string.IsNullOrWhiteSpace(pwd))
        {
            Log.Warning("Empty passphrase provided; aborting for security");
            return;
        }

        freelan.SetPassphrase(pwd);
        freelan.SetMode(FreeLanMode.HOST);
        freelan.SetRelayMode(relay);
        freelan.SetShowShell(debug);

        var ok = freelan.StartFreeLan();
        if (!ok)
        {
            Log.Error("HOST failed to start");
            return;
        }

        Log.Information("Peer count: {Count}", tracker.PeerCount);
        Console.WriteLine("Press Ctrl+C to stop...");
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; freelan.StopFreeLan(); };
        Thread.Sleep(Timeout.Infinite);
    }
    catch (InvalidInput ex)
    {
        Log.Error(ex, "Invalid input");
    }
    catch (OperationCanceledException)
    {
        // graceful shutdown
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Unhandled error in host command");
        throw;
    }
    finally
    {
        Log.CloseAndFlush();
    }
}, passwordOption, relayOption, debugOption, logLevelOption);

Command stopCmd = new("stop", "Stop any running freelan process");
stopCmd.SetHandler(() =>
{
    ConfigureLogging("Information");
    var killed = FreeLanService.KillStrangeFreeLan();
    Log.Information(killed ? "Stopped freelan" : "No freelan process found");
    Log.CloseAndFlush();
});

Command statusCmd = new("status", "Show freelan status");
statusCmd.SetHandler(() =>
{
    ConfigureLogging("Information");

    // Read recent raw log lines first
    var recentLines = ReadRecentLogLines("logs/server.log", 1000);

    // Analyze peer-related events by replaying log lines through PeerTracker
    var tracker = new PeerTracker();
    foreach (var line in recentLines)
    {
        tracker.ProcessLine(line);
    }

    // Discover listening ports (best-effort)
    System.Net.NetworkInformation.IPGlobalProperties ipProps = System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties();
    var tcpListeners = ipProps.GetActiveTcpListeners().Select(p => p.Port).OrderBy(p => p).ToArray();
    var udpListeners = ipProps.GetActiveUdpListeners().Select(p => p.Port).OrderBy(p => p).ToArray();

    // Recent peer events and recent errors
    var recentEvents = tracker.Recent(50).Select(e => new { e.Time, e.Type, e.PeerIp, e.Reason }).ToArray();
    var recentErrors = recentEvents.Where(e => string.Equals(e.Type, "error", StringComparison.OrdinalIgnoreCase)).ToArray();

    var any = FreeLanService.GetStrangeFreeLansRunning();

    var result = new
    {
        Running = any,
        Timestamp = DateTime.UtcNow,
        PeerCount = tracker.PeerCount,
        OpenTcpPorts = tcpListeners,
        OpenUdpPorts = udpListeners,
        RecentPeerEvents = recentEvents,
        RecentErrors = recentErrors,
        RecentLogLines = recentLines
    };

    JsonSerializerOptions options = s_jsonOptions;

    var json = JsonSerializer.Serialize(result, options);
    Console.WriteLine(json);

    Log.Information(any ? "Running" : "Stopped");
    Log.CloseAndFlush();
});

RootCommand root = new("P2PLauncher Server CLI");
root.AddCommand(hostCmd);
root.AddCommand(stopCmd);
root.AddCommand(statusCmd);

return await root.InvokeAsync(args).ConfigureAwait(false);

void ConfigureLogging(string level)
{
    LogEventLevel logEvent = Enum.TryParse(level, true, out LogEventLevel lvl) ? lvl : LogEventLevel.Information;
    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Is(logEvent)
        .Enrich.FromLogContext()
        .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
        .WriteTo.File(path: "logs/server.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7, formatProvider: CultureInfo.InvariantCulture)
        .CreateLogger();
}

string[] ReadRecentLogLines(string path, int maxLines)
{
    try
    {
        if (!File.Exists(path))
        {
            return Array.Empty<string>();
        }
        // Read all lines but keep only last maxLines to avoid large memory
        var all = File.ReadAllLines(path);
        if (all.Length <= maxLines)
        {
            return all;
        }

        var result = new string[maxLines];
        Array.Copy(all, all.Length - maxLines, result, 0, maxLines);
        return result;
    }
    catch (IOException ex)
    {
        Log.Warning(ex, "Unable to read log file: {Path}", path);
        return new[] { "<unable to read log file>" };
    }
    catch (UnauthorizedAccessException ex)
    {
        Log.Warning(ex, "Access denied reading log file: {Path}", path);
        return new[] { "<access denied reading log file>" };
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Unexpected exception while reading log file: {Path}", path);
        throw;
    }
}
