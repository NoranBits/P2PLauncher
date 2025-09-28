using System.CommandLine;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net.NetworkInformation;
using Serilog;
using Serilog.Formatting.Compact;
using P2PLauncher.Server.Models;

// Configure JSON serialization with UTC support
var jsonOptions = new JsonSerializerOptions
{
    WriteIndented = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    Converters = { new UtcDateTimeConverter() }
};

// Global cancellation token for graceful shutdown
using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => 
{
    e.Cancel = true;
    cts.Cancel();
};

// Configure Serilog with compact JSON formatter
ConfigureSerilog();

// Setup commands
var runCommand = new Command("run", "Start the headless server loop");
runCommand.SetHandler(async () => await RunServerAsync(cts.Token));

var statusCommand = new Command("status", "Print server status as JSON to stdout");
statusCommand.SetHandler(async () => await PrintStatusAsync());

var stopCommand = new Command("stop", "Graceful shutdown hook");
stopCommand.SetHandler(async () => await StopServerAsync());

var testPortsCommand = new Command("test-ports", "Test port detection and return sample arrays");
testPortsCommand.SetHandler(async () => await TestPortsAsync());

var printConfigCommand = new Command("print-config", "Echo current effective configuration");
printConfigCommand.SetHandler(async () => await PrintConfigAsync());

// Root command
var rootCommand = new RootCommand("P2PLauncher Server - Headless server CLI")
{
    runCommand,
    statusCommand,
    stopCommand,
    testPortsCommand,
    printConfigCommand
};

try
{
    return await rootCommand.InvokeAsync(args);
}
catch (OperationCanceledException)
{
    Log.Information("Operation was cancelled");
    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}

async Task RunServerAsync(CancellationToken cancellationToken)
{
    Log.Information("Starting P2PLauncher headless server");
    
    try
    {
        // Placeholder timer-based processing
        using var timer = new Timer(async _ => 
        {
            Log.Information("Server heartbeat - simulating peer discovery and processing");
            
            // Simulate some work
            await Task.Delay(100, cancellationToken);
        }, null, TimeSpan.Zero, TimeSpan.FromSeconds(30));

        Log.Information("Server is running. Press Ctrl+C to stop.");
        
        // Wait until cancellation is requested
        await Task.Delay(Timeout.Infinite, cancellationToken);
    }
    catch (OperationCanceledException)
    {
        Log.Information("Server shutdown requested");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error occurred during server operation");
        throw;
    }
    
    Log.Information("Server stopped");
}

async Task PrintStatusAsync()
{
    Log.Debug("Generating server status");
    
    try
    {
        var status = await GenerateServerStatusAsync();
        var json = JsonSerializer.Serialize(status, jsonOptions);
        Console.WriteLine(json);
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Failed to generate server status");
        throw;
    }
}

async Task StopServerAsync()
{
    Log.Information("Graceful shutdown initiated");
    cts.Cancel();
    
    // Simulate cleanup work
    await Task.Delay(100);
    
    Log.Information("Shutdown complete");
}

async Task TestPortsAsync()
{
    Log.Information("Testing port detection");
    
    try
    {
        var (tcpPorts, udpPorts) = await GetOpenPortsAsync();
        
        var result = new
        {
            TcpPorts = tcpPorts,
            UdpPorts = udpPorts,
            SampleTcpPorts = new[] { 80, 443, 8080, 9000 },
            SampleUdpPorts = new[] { 53, 67, 68, 123, 12000 }
        };
        
        var json = JsonSerializer.Serialize(result, jsonOptions);
        Console.WriteLine(json);
        
        Log.Information("Port detection completed - TCP: {TcpCount}, UDP: {UdpCount}", 
            tcpPorts.Length, udpPorts.Length);
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Failed to test ports");
        throw;
    }
}

async Task PrintConfigAsync()
{
    Log.Information("Printing current effective configuration");
    
    var config = new
    {
        ServerName = "P2PLauncher.Server",
        Version = "1.0.0",
        TargetFramework = "net9.0",
        LogLevel = "Information",
        LogOutputs = new[] { "Console (CompactJSON)", "File (Rolling)" },
        JsonOptions = new
        {
            WriteIndented = true,
            PropertyNamingPolicy = "CamelCase",
            UtcDateTimeHandling = true
        },
        Commands = new[] { "run", "status", "stop", "test-ports", "print-config" },
        CancellationSupport = true
    };
    
    var json = JsonSerializer.Serialize(config, jsonOptions);
    Console.WriteLine(json);
    
    await Task.CompletedTask;
}

async Task<ServerStatus> GenerateServerStatusAsync()
{
    var now = DateTime.UtcNow;
    var (tcpPorts, udpPorts) = await GetOpenPortsAsync();
    
    // Sample data for demonstration
    var samplePeers = new[]
    {
        new PeerInfo 
        { 
            Ip = "192.168.1.100", 
            Id = "peer001", 
            Name = "TestPeer1", 
            Since = now.AddMinutes(-15) 
        },
        new PeerInfo 
        { 
            Ip = "192.168.1.101", 
            Id = "peer002", 
            Name = "TestPeer2", 
            Since = now.AddMinutes(-5) 
        }
    };
    
    var sampleEvents = new[]
    {
        new PeerEvent
        {
            Time = now.AddMinutes(-10),
            Type = "joined",
            PeerIp = "192.168.1.100",
            PeerId = "peer001",
            PeerName = "TestPeer1",
            Reason = "Connection established"
        },
        new PeerEvent
        {
            Time = now.AddMinutes(-2),
            Type = "info",
            PeerIp = "192.168.1.101", 
            PeerId = "peer002",
            PeerName = "TestPeer2",
            Reason = "Heartbeat received"
        }
    };
    
    var logLines = await ReadRecentLogLinesAsync();
    
    return new ServerStatus
    {
        Running = !cts.Token.IsCancellationRequested,
        Timestamp = now,
        PeerCount = samplePeers.Length,
        CurrentPeers = samplePeers,
        RecentPeerEvents = sampleEvents,
        OpenTcpPorts = tcpPorts,
        OpenUdpPorts = udpPorts,
        RecentSavedHosts = new[] { "host1.example.com", "192.168.1.1", "host2.example.com" },
        RecentLogLines = logLines
    };
}

async Task<(int[] tcpPorts, int[] udpPorts)> GetOpenPortsAsync()
{
    await Task.CompletedTask;
    
    try
    {
        var ipProps = IPGlobalProperties.GetIPGlobalProperties();
        var tcpListeners = ipProps.GetActiveTcpListeners().Select(ep => ep.Port).Distinct().OrderBy(p => p).ToArray();
        var udpListeners = ipProps.GetActiveUdpListeners().Select(ep => ep.Port).Distinct().OrderBy(p => p).ToArray();
        
        return (tcpListeners, udpListeners);
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Failed to get network port information, returning empty arrays");
        return ([], []);
    }
}

async Task<string[]> ReadRecentLogLinesAsync()
{
    await Task.CompletedTask;
    
    var logPath = Path.Combine("logs", $"server-{DateTime.Now:yyyyMMdd}.log");
    
    try
    {
        if (!File.Exists(logPath))
        {
            return new[] { "Log file not found", "Server starting up..." };
        }
        
        var lines = await File.ReadAllLinesAsync(logPath);
        return lines.TakeLast(10).ToArray();
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Failed to read log file: {LogPath}", logPath);
        return new[] { $"Error reading log file: {ex.Message}" };
    }
}

void ConfigureSerilog()
{
    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Information()
        .Enrich.FromLogContext()
        .WriteTo.Console(new CompactJsonFormatter())
        .WriteTo.File(
            new CompactJsonFormatter(),
            path: Path.Combine("logs", "server-.log"),
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 7)
        .CreateLogger();
    
    Log.Information("Serilog configured with compact JSON formatting");
}
