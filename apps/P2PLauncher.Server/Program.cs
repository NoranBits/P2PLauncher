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
TaskScheduler.UnobservedTaskException += (s, e) =>
{
    if (e?.Exception != null)
    {
        Log.Error(e.Exception, "UnobservedTaskException");
    }
    e?.SetObserved();
};

var passwordOption = new Option<string>("--password", "Passphrase");
var relayOption = new Option<bool>("--relay", "Enable relay mode");
var debugOption = new Option<bool>("--debug", "Show freelan console window");
var logLevelOption = new Option<string>("--log-level", "Log level (Verbose|Debug|Information|Warning|Error|Fatal)");

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
        var windowsServices = new WindowsServices();
        var dialog = new WinDialogService();
        var files = new WinFileService();
        var detect = new FreeLanDetectionService(files, dialog);
        var freelan = new FreeLanService(windowsServices, detect, dialog);
        var tracker = new PeerTracker();
        var prefs = new ServerPreferencesService();

        freelan.OutputReceived += line => { Log.Debug("freelan: {Line}", line); tracker.ProcessLine(line); };
        freelan.ServiceStarted += () => Log.Information("freelan started");
        freelan.ServiceStopped += () =>
        {
            Log.Information("freelan stopped");
            // clear cached identities when freelan stops (server restart/shutdown)
            try
            {
                PeerTracker.ClearAllIds();
            }
            catch (IOException ex)
            {
                Log.Warning(ex, "Unable to clear peer identities (IO)");
            }
            catch (UnauthorizedAccessException ex)
            {
                Log.Warning(ex, "Unable to clear peer identities (ACL)");
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Unable to clear peer identities");
            }
        };

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

        // Persist successful host start so operators can see known hosts in server status
        try
        {
            prefs.SaveSuccessfulConnection("9.0.0.1", 12000);
        }
        catch (IOException ex)
        {
            Log.Warning(ex, "Unable to persist server successful connection (IO)");
        }
        catch (UnauthorizedAccessException ex)
        {
            Log.Warning(ex, "Unable to persist server successful connection (ACL)");
        }
        catch (JsonException ex)
        {
            Log.Warning(ex, "Unable to persist server successful connection (JSON)");
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
    catch (IOException ex)
    {
        Log.Error(ex, "IO error in host command");
        throw;
    }
    catch (UnauthorizedAccessException ex)
    {
        Log.Error(ex, "Access error in host command");
        throw;
    }
    catch (JsonException ex)
    {
        Log.Error(ex, "JSON error in host command");
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

    // Also clear the cached identities since server is stopping
    try
    {
        PeerTracker.ClearAllIds();
    }
    catch (IOException ex)
    {
        Log.Warning(ex, "Unable to clear peer identities on stop command (IO)");
    }
    catch (UnauthorizedAccessException ex)
    {
        Log.Warning(ex, "Unable to clear peer identities on stop command (ACL)");
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Unable to clear peer identities on stop command");
    }

    Log.CloseAndFlush();
});

Command clearIdsCmd = new("clear-ids", "Clear cached peer identities")
{
};
clearIdsCmd.SetHandler(() =>
{
    ConfigureLogging("Information");
    try
    {
        PeerTracker.ClearAllIds();
        Log.Information("Cleared peer identities");
    }
    catch (IOException ex)
    {
        Log.Warning(ex, "Unable to clear peer identities (IO)");
    }
    catch (UnauthorizedAccessException ex)
    {
        Log.Warning(ex, "Unable to clear peer identities (ACL)");
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Unable to clear peer identities");
    }
    Log.CloseAndFlush();
});

Command statusCmd = new("status", "Show freelan status");
statusCmd.SetHandler(() =>
{
    ConfigureLogging("Information");

    try
    {
        // Read recent raw log lines first
        var recentLines = ReadRecentLogLines("logs/server.log", 1000);

        // Analyze peer-related events by replaying log lines through PeerTracker
        var tracker = new PeerTracker();
        foreach (var line in recentLines)
        {
            tracker.ProcessLine(line);
        }

        // Discover listening ports (best-effort)
        var ipProps = System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties();
        var tcpListeners = ipProps.GetActiveTcpListeners().Select(p => p.Port).OrderBy(p => p).ToArray();
        var udpListeners = ipProps.GetActiveUdpListeners().Select(p => p.Port).OrderBy(p => p).ToArray();

        // Recent peer events and recent errors
        var recentEvents = tracker.Recent(50).Select(e => new { e.Time, e.Type, e.PeerIp, e.PeerId, e.PeerName, e.Reason });
        var recentErrors = recentEvents.Where(x => string.Equals(x.Type, "error", StringComparison.OrdinalIgnoreCase));

        var any = FreeLanService.GetStrangeFreeLansRunning();

        // Include recent saved hosts from server preferences
        var prefs = new ServerPreferencesService();
        IReadOnlyList<string> recentSavedHosts = prefs.GetRecentHosts(50);

        // Add current peers with identity
        IReadOnlyList<PeerInfo> currentPeers = tracker.GetCurrentPeers();

        var result = new
        {
            Running = any,
            Timestamp = DateTime.UtcNow,
            PeerCount = tracker.PeerCount,
            OpenTcpPorts = tcpListeners,
            OpenUdpPorts = udpListeners,
            CurrentPeers = currentPeers,
            RecentPeerEvents = recentEvents,
            RecentErrors = recentErrors,
            RecentLogLines = recentLines,
            RecentSavedHosts = recentSavedHosts
        };

        var json = JsonSerializer.Serialize(result, s_jsonOptions);
        Console.WriteLine(json);

        Log.Information(any ? "Running" : "Stopped");
    }
    catch (IOException ex)
    {
        Log.Error(ex, "IO error in status command");
        throw;
    }
    catch (UnauthorizedAccessException ex)
    {
        Log.Error(ex, "Access error in status command");
        throw;
    }
    catch (JsonException ex)
    {
        Log.Error(ex, "JSON error in status command");
        throw;
    }
    finally
    {
        Log.CloseAndFlush();
    }
});

RootCommand root = new("P2PLauncher Server CLI");
root.AddCommand(hostCmd);
root.AddCommand(stopCmd);
root.AddCommand(statusCmd);
root.AddCommand(clearIdsCmd);

return await root.InvokeAsync(args).ConfigureAwait(false);

void ConfigureLogging(string level)
{
    var logEvent = Enum.TryParse(level, true, out LogEventLevel lvl) ? lvl : LogEventLevel.Information;
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
            return [];
        }

        // Use streaming + LINQ to keep last maxLines without manual array copying
        var lines = File.ReadLines(path).Reverse().Take(maxLines).Reverse().ToArray();
        return lines;
    }
    catch (IOException ex)
    {
        Log.Warning(ex, "Unable to read log file: {Path}", path);
        return ["<unable to read log file>"];
    }
    catch (UnauthorizedAccessException ex)
    {
        Log.Warning(ex, "Access denied reading log file: {Path}", path);
        return ["<access denied reading log file>"];
    }
}
