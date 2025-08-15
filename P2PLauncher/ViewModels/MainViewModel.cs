using System;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using P2PLauncher.Model;
using P2PLauncher.Services;

namespace P2PLauncher.ViewModels
{
    /// <summary>
    /// Main application view-model (MVVM Toolkit). Provides basic bindings and commands.
    /// </summary>
    [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated via XAML DataContext")]
    internal sealed partial class MainViewModel : ObservableObject, IDisposable
    {
        private readonly FreeLanService freelan;
        private readonly ClientPreferencesService prefs;

        [ObservableProperty]
        private string host = string.Empty;

        [ObservableProperty]
        private int port = 12000;

        [ObservableProperty]
        private string status = "Idle";

        [ObservableProperty]
        private bool debugEnabled;

        public ObservableCollection<string> RecentHosts { get; } = [];

        public MainViewModel()
        {
            // services
            var windowsServices = new WindowsServices();
            var dialog = new WinDialogService();
            var files = new WinFileService();
            var detect = new FreeLanDetectionService(files, dialog);
            freelan = new FreeLanService(windowsServices, detect, dialog);
            prefs = new ClientPreferencesService();

            // load recents
            foreach (var item in prefs.LoadRecentHosts(20))
            {
                RecentHosts.Add(item);
            }

            // default
            if (RecentHosts.Count == 0)
            {
                RecentHosts.Add("9.0.0.1:12000");
            }

            freelan.ServiceStarted += () => Status = "Connected";
            freelan.ServiceStopped += () => Status = "Stopped";
            freelan.OutputReceived += line => { /* optional: surface logs later */ };
        }

        private bool ValidateInputs()
        {
            if (string.IsNullOrWhiteSpace(Host))
            {
                Status = "Host is required";
                return false;
            }
            if (Port is < 1 or > 65535)
            {
                Status = "Port must be 1-65535";
                return false;
            }
            return true;
        }

        [RelayCommand]
        private void Connect()
        {
            if (!ValidateInputs())
            {
                return;
            }

            // minimal validation per mode
            freelan.SetMode(FreeLanMode.CLIENT);
            freelan.SetShowShell(DebugEnabled);
            freelan.SetRelayMode(false);

            try
            {
                freelan.SetHostIp(Host);
                Status = $"Connecting to {Host}:{Port}...";
                var ok = freelan.StartFreeLan();
                if (ok)
                {
                    prefs.SaveSuccessfulConnection(Host, Port);
                    var entry = $"{Host}:{Port}";
                    if (!RecentHosts.Contains(entry))
                    {
                        RecentHosts.Insert(0, entry);
                    }
                }
                else
                {
                    Status = "Failed to start";
                }
            }
            catch (Exceptions.InvalidInput ex)
            {
                Status = ex.Message;
            }
            catch (IOException ex)
            {
                Status = $"IO error: {ex.Message}";
            }
            catch (UnauthorizedAccessException ex)
            {
                Status = $"Access error: {ex.Message}";
            }
        }

        [RelayCommand]
        private void Stop()
        {
            try
            {
                freelan.StopFreeLan();
                Status = "Stopped";
            }
            catch (InvalidOperationException ex)
            {
                Status = ex.Message;
            }
        }

        public void Dispose()
        {
            freelan.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
