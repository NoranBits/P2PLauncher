using System;
using System.CommandLine;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using P2PLauncher.Exceptions;
using P2PLauncher.Model;
using P2PLauncher.Services;
using Serilog;
using Serilog.Events;

namespace P2PLauncher.Server
{
    internal static class Program
    {
        private static async Task<int> Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) => Log.Fatal(e.ExceptionObject as Exception, "UnhandledException");
            TaskScheduler.UnobservedTaskException += (s, e) => { if (e?.Exception != null) Log.Error(e.Exception, "UnobservedTaskException"); e?.SetObserved(); };

            var passwordOption = new Option<string>(name: "--password", description: "Passphrase", getDefaultValue: () => string.Empty);
            var relayOption = new Option<bool>(name: "--relay", description: "Enable relay mode", getDefaultValue: () => false);
            var debugOption = new Option<bool>(name: "--debug", description: "Show freelan console window", getDefaultValue: () => false);
            var logLevelOption = new Option<string>(name: "--log-level", description: "Log level (Verbose|Debug|Information|Warning|Error|Fatal)", getDefaultValue: () => "Information");

            var hostCmd = new Command("host", "Start FreeLAN in host mode")
            {
                passwordOption, relayOption, debugOption, logLevelOption
            };
            hostCmd.SetHandler((string pwd, bool relay, bool debug, string logLevel) =>
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

                    bool ok = freelan.StartFreeLan();
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

            var stopCmd = new Command("stop", "Stop any running freelan process");
            stopCmd.SetHandler(() =>
            {
                ConfigureLogging("Information");
                bool killed = FreeLanService.KillStrangeFreeLan();
                Log.Information(killed ? "Stopped freelan" : "No freelan process found");
                Log.CloseAndFlush();
            });

            var statusCmd = new Command("status", "Show freelan status");
            statusCmd.SetHandler(() =>
            {
                ConfigureLogging("Information");
                bool any = FreeLanService.GetStrangeFreeLansRunning();
                Log.Information(any ? "Running" : "Stopped");
                Console.WriteLine("See logs/server.log for peer events.");
                Log.CloseAndFlush();
            });

            var root = new RootCommand("P2PLauncher Server CLI");
            root.AddCommand(hostCmd);
            root.AddCommand(stopCmd);
            root.AddCommand(statusCmd);

            return await root.InvokeAsync(args).ConfigureAwait(false);
        }

        private static void ConfigureLogging(string level)
        {
            var logEvent = Enum.TryParse<LogEventLevel>(level, true, out var lvl) ? lvl : LogEventLevel.Information;
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Is(logEvent)
                .Enrich.FromLogContext()
                .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
                .WriteTo.File(path: "logs/server.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7, formatProvider: CultureInfo.InvariantCulture)
                .CreateLogger();
        }
    }
}
