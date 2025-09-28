using Microsoft.Extensions.DependencyInjection;
using P2PLauncher.Client.ViewModels;
using System.Windows;

namespace P2PLauncher.Client.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly IServiceProvider _serviceProvider;

    public MainWindow(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        InitializeComponent();
        SetupViewModel();
    }

    private void SetupViewModel()
    {
        // Create a composite view model that exposes both Dashboard and Diagnostics
        var dashboardViewModel = _serviceProvider.GetRequiredService<DashboardViewModel>();
        var diagnosticsViewModel = _serviceProvider.GetRequiredService<DiagnosticsViewModel>();
        
        // Set DataContext to an anonymous object that exposes both ViewModels
        DataContext = new
        {
            Dashboard = dashboardViewModel,
            Diagnostics = diagnosticsViewModel
        };
        
        // Set individual tab DataContexts
        if (FindName("DashboardTab") is FrameworkElement dashboardTab)
            dashboardTab.DataContext = dashboardViewModel;
            
        if (FindName("DiagnosticsTab") is FrameworkElement diagnosticsTab)
            diagnosticsTab.DataContext = diagnosticsViewModel;
    }

    protected override void OnClosed(EventArgs e)
    {
        // Dispose ViewModels if they implement IDisposable
        if (DataContext is { Dashboard: IDisposable dashboard })
            dashboard.Dispose();
            
        if (DataContext is { Diagnostics: IDisposable diagnostics })
            diagnostics.Dispose();
            
        base.OnClosed(e);
    }
}