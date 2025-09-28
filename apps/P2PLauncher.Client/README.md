# P2PLauncher.Client

A modern WPF client application built with .NET 9, following MVVM pattern and using Generic Host for dependency injection.

## Features

- **Modern WPF Architecture**: Built with CommunityToolkit.Mvvm for clean MVVM implementation
- **Dependency Injection**: Uses Microsoft.Extensions.Hosting for service registration and resolution
- **Logging**: Integrated Serilog with file rotation and console output
- **Async Operations**: All operations support cancellation tokens for responsive UI
- **Network Diagnostics**: Built-in ICMP ping and TCP connection testing
- **Real-time Logging**: Observable log stream with thread-safe UI updates

## Architecture

### Project Structure
```
apps/P2PLauncher.Client/
├── App.xaml(.cs)                    # Application bootstrap with Generic Host
├── Views/
│   └── MainWindow.xaml(.cs)         # Main UI with tabbed interface
├── ViewModels/
│   ├── DashboardViewModel.cs        # P2P connection management
│   └── DiagnosticsViewModel.cs      # Network diagnostic tools
└── Services/
    ├── IFeedbackService.cs          # Logging service interface
    ├── FeedbackService.cs           # File-based logging with rotation
    ├── IDiagnosticsService.cs       # Network diagnostics interface
    └── DiagnosticsService.cs        # ICMP ping and TCP connect tests
```

### Dependencies
- **CommunityToolkit.Mvvm** 8.3.2 - MVVM framework with source generators
- **Microsoft.Extensions.Hosting** 9.0.1 - Generic Host and DI container
- **Serilog** 3.1.1 - Structured logging framework
- **System.Reactive** 6.0.0 - Observable pattern for real-time log updates

## Building and Running

### Prerequisites
- .NET 9.0 SDK
- Windows 10.0.19041.0 or later
- Visual Studio 2022 or Rider

### Build
```bash
dotnet build apps/P2PLauncher.Client/P2PLauncher.Client.csproj
```

### Run
```bash
dotnet run --project apps/P2PLauncher.Client/P2PLauncher.Client.csproj
```

## Manual Testing Checklist

### Dashboard Tab
- [ ] Application starts without errors
- [ ] Dashboard shows "Status: Disconnected" initially
- [ ] "Connected Peers: 0" displayed
- [ ] Start Host button is enabled
- [ ] Connect button is enabled
- [ ] Disconnect button is disabled
- [ ] Click "Start Host" - status changes to "Starting Host..." then "Host Active"
- [ ] Peer count remains 0 for host mode
- [ ] Log entries appear in Recent Activity list
- [ ] Click "Connect" - status changes to "Connecting..." then "Connected"
- [ ] Peer count shows random number (1-10) when connected
- [ ] Click "Disconnect" - status changes to "Disconnecting..." then "Disconnected"
- [ ] Button states update correctly based on connection status

### Diagnostics Tab
- [ ] Default ping hostname is "8.8.8.8"
- [ ] Ping button enabled initially
- [ ] Enter valid hostname and click Ping
- [ ] Result shows success/failure with timing
- [ ] During ping, button is disabled
- [ ] Default TCP hostname is "google.com", port "80"
- [ ] TCP Test button enabled initially
- [ ] Click Test button - shows connection result with timing
- [ ] During test, button is disabled
- [ ] Invalid port numbers show validation errors

### Logging and Error Handling
- [ ] Log files created in `logs/` directory
- [ ] Log rotation occurs when file exceeds 10MB
- [ ] All user actions logged to both file and Recent Activity
- [ ] Unhandled exceptions show error dialog
- [ ] Operations can be cancelled (no explicit cancel button yet)
- [ ] UI remains responsive during long operations

### Edge Cases and Thread Safety
- [ ] Rapid clicking buttons doesn't cause exceptions
- [ ] Log list never exceeds 100 entries (old entries removed)
- [ ] All UI updates occur on UI thread
- [ ] Service disposal on app shutdown doesn't throw exceptions
- [ ] Invalid network operations handled gracefully

## XAML Binding Examples

### Dashboard Commands
```xml
<Button Content="Start Host" Command="{Binding StartHostCommand}" />
<Button Content="Connect" Command="{Binding ConnectCommand}" />
<Button Content="Disconnect" Command="{Binding DisconnectCommand}" />
```

### Observable Properties
```xml
<TextBlock Text="{Binding ConnectionState, StringFormat='Status: {0}'}" />
<TextBlock Text="{Binding PeerCount, StringFormat='Connected Peers: {0}'}" />
<ListBox ItemsSource="{Binding RecentLogLines}" />
```

### Two-way Binding
```xml
<TextBox Text="{Binding PingHostname, UpdateSourceTrigger=PropertyChanged}" />
<TextBox Text="{Binding TcpPort, UpdateSourceTrigger=PropertyChanged}" />
```

## Service Registration (in App.xaml.cs)
```csharp
.ConfigureServices((context, services) =>
{
    // Register services
    services.AddSingleton<IFeedbackService, FeedbackService>();
    services.AddSingleton<IDiagnosticsService, DiagnosticsService>();

    // Register ViewModels
    services.AddTransient<DashboardViewModel>();
    services.AddTransient<DiagnosticsViewModel>();

    // Register Views
    services.AddSingleton<MainWindow>();
});
```