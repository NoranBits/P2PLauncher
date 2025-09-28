using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using P2PLauncher.Client.Services;
using P2PLauncher.Client.ViewModels;
using P2PLauncher.Client.Views;
using Serilog;
using System.Windows;

namespace P2PLauncher.Client;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        try
        {
            // Configure Serilog
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File("logs/p2plauncher-client-.log", 
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7)
                .CreateLogger();

            // Create and configure the host
            _host = CreateHostBuilder().Build();

            // Start the host
            await _host.StartAsync();

            // Create and show the main window
            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application startup failed");
            MessageBox.Show($"Application failed to start: {ex.Message}", "Startup Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }

        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host != null)
        {
            await _host.StopAsync(TimeSpan.FromSeconds(5));
            _host.Dispose();
        }
        
        Log.CloseAndFlush();
        base.OnExit(e);
    }

    private static IHostBuilder CreateHostBuilder()
    {
        return Host.CreateDefaultBuilder()
            .UseSerilog()
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
    }
}