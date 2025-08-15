using P2PLauncher.Exceptions;
using P2PLauncher.Model;
using P2PLauncher.Services;
using P2PLauncher.Utils;
using System.Globalization;
using System.Windows;
using System.Windows.Threading;
using System.Diagnostics.CodeAnalysis;

namespace P2PLauncher.View
{
    [SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "WPF Window type referenced by XAML and app entrypoint.")]
    /// <summary>
    /// Interaction logic for MainLauncherWindow.xaml
    /// </summary>
    public sealed partial class MainLauncherWindow : Window, IWindow, IDisposable
    {
        private readonly FreeLanDetectionService freeLanDetectionService;
        private readonly NetworkAdapters networkAdapters;
        private readonly WindowsServices windowsServices;
        private readonly IFileService fileService;
        private readonly IDialogService dialogService;
        private readonly FreeLanService freeLanService;
        private readonly UserPreferencesService userPrefs = new();
        private DispatcherTimer processCheck = new();
        private DispatcherTimer freeLanAddressCheck = new();
        private bool _disposed;


        public MainLauncherWindow()
        {
            InitializeComponent();

            fileService = new WinFileService();
            dialogService = new WinDialogService();
            freeLanDetectionService = new FreeLanDetectionService(fileService, dialogService);
            windowsServices = new WindowsServices();
            networkAdapters = new NetworkAdapters();
            freeLanService = new FreeLanService(
                windowsServices,
                freeLanDetectionService,
                dialogService
                );



            if (!EnvHelper.IsAdministrator())
            {
                _ = MessageBox.Show("To use this application you will need administrator privileges!");
                Environment.Exit(0);
            }

            // Load saved client defaults
            UserPreferencesService.ClientDefaults saved = userPrefs.LoadClient();
            TextBoxClientHost.Text = string.IsNullOrWhiteSpace(saved.Host) ? TextBoxClientHost.Text : saved.Host;
            TextBoxClientPassword.Text = string.IsNullOrWhiteSpace(saved.Password) ? TextBoxClientPassword.Text : saved.Password;
            TextBoxId.Text = string.IsNullOrWhiteSpace(saved.Id) ? TextBoxId.Text : saved.Id;
            CheckBoxClientRelay.IsChecked = saved.Relay;
            CheckBoxDebug.IsChecked = saved.ShowDebug;

            // Attempt to prefill host IPv4 from clipboard if empty and looks like IPv4
            if (string.IsNullOrWhiteSpace(TextBoxClientHost.Text) && Clipboard.ContainsText())
            {
                var txt = Clipboard.GetText();
                if (TryGetIPv4(txt, out var ip))
                {
                    TextBoxClientHost.Text = ip;
                }
            }

            UpdateWindow();

        }

        #region UI setters
        private void SetFreeLanStatusValueLabel(string content)
        {
            LabelFreeLanStatusValue.Content = content;
        }
        private void SetServicesToDisableValueLabel(string content)
        {
            LabelServicesToDisableValue.Content = content;
        }
        private void SetAdaptersToDisableValueLabel(string content)
        {
            LabelAdaptersToDisableValue.Content = content;
        }
        private void SetPublicAddressValueLabel(string content)
        {
            LabelPublicAddress.Content = content;
        }
        private void SetFreeLANAddressValueLabel(string content)
        {
            LabelFreeLANAddress.Content = content;
        }
        private void SetVisibilityFreeLANAddressTipLabel(bool visible)
        {
            LabelFreeLANAddressTip.Visibility = visible ? Visibility.Visible : Visibility.Hidden;
        }
        private void SetStateValueLabel(string content)
        {
            LabelStateValue.Content = content;
        }
        private static void SetDonatorsLabel()
        {
            // Removed Donators UI from XAML; keep method for compatibility, but no-op
        }
        #endregion

        #region UI
        public void UpdateFreeLanStatus()
        {
            FreeLanInstallationStatus status = freeLanDetectionService.GetInstallationStatus();
            SetFreeLanStatusValueLabel(EnumHelper.GetDescription(status));
        }
        public void UpdateNumbers()
        {
            SetAdaptersToDisableValueLabel(NetworkAdapters.GetAdapterNamesToDisable().Length.ToString(CultureInfo.InvariantCulture));
            SetServicesToDisableValueLabel(windowsServices.GetServiceNamesToDisable().Length.ToString(CultureInfo.InvariantCulture));
            SetPublicAddressValueLabel(EnvHelper.GetPublicAddress());



        }

        private bool UpdateFreeLANAddress()
        {
            List<string> tapIps = [.. NetworkAdapters.GetTAPCurrentIP()];
            for (var i = tapIps.Count - 1; i >= 0; i--)
            {
                if (!freeLanService.IsThisValidIPForTheCurrentMode(tapIps[i]))
                {
                    tapIps.RemoveAt(i);
                }
            }
            if (tapIps.Count == 0)
            {
                SetFreeLANAddressValueLabel("Unknown.");
                SetVisibilityFreeLANAddressTipLabel(false);
                return false;
            }
            else if (tapIps.Count > 1)
            {
                SetFreeLANAddressValueLabel(string.Join(",", tapIps.ToArray()));
                SetVisibilityFreeLANAddressTipLabel(true);
            }
            else
            {
                SetFreeLANAddressValueLabel(tapIps[0]);
                SetVisibilityFreeLANAddressTipLabel(false);
            }
            return true;
        }

        public void UpdateWindow()
        {
            UpdateFreeLanStatus();
            UpdateNumbers();
            SetDonatorsLabel();

        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (freeLanService.IsRunning)
            {
                OnFreeLanStop();
            }
            Dispose();
        }

        #endregion

        #region Button actions (all)

        private void OnOpenLogsClick(object sender, RoutedEventArgs e)
        {
            UpdateWindow();
            if (EnvHelper.FileExists("debug.txt"))
            {
                EnvHelper.OpenNotepadWithFile("debug.txt");
            }
            else
            {
                _ = MessageBox.Show("There are no logs yet");
            }
        }

        private void OnOpenFreeLanSettingsButton(object sender, RoutedEventArgs e)
        {
            FreeLanDetectionWindow window = new();
            _ = window.ShowDialog();
            UpdateWindow();
        }
        private void OnOpenAdaptersSettingsButton(object sender, RoutedEventArgs e)
        {
            NetworkAdaptersWindow window = new();
            _ = window.ShowDialog();
            UpdateWindow();
        }
        private void OnOpenServicesSettingsButton(object sender, RoutedEventArgs e)
        {
            WindowsServicesWindow window = new();
            _ = window.ShowDialog();
            UpdateWindow();
        }
        private void OnCopyPublicAddressClick(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(LabelPublicAddress.Content.ToString());
        }

        #endregion

        #region Button actions Tabs
        private void OnCommonStart()
        {
            if (freeLanService.GetFreeLanServiceStatus())
            {
                _ = MessageBox.Show("FreeLAN is already running in the background. To prevent this app from failing, it will be disabled.");
            }
            foreach (WindowsService w in windowsServices.GetServicesToDisable())
            {
                w.Disable();
            }
            foreach (NetworkAdapter a in networkAdapters.GetAdaptersToDisable())
            {
                a.Disable();
            }

            StartFreeLANAddressCheck();
        }

        private void OnHubStartClick(object sender, RoutedEventArgs e)
        {
            _ = MessageBox.Show("Hub mode is no longer available in the simplified UI.");
        }

        private void OnHostStartClick(object sender, RoutedEventArgs e)
        {
            try
            {
                freeLanService.SetMode(FreeLanMode.HOST);
                freeLanService.SetPassphrase(TextBoxHostPassword.Text);
                freeLanService.SetRelayMode(CheckBoxHostRelay.IsChecked.GetValueOrDefault());
                freeLanService.SetShowShell(CheckBoxDebug.IsChecked.GetValueOrDefault());
                _ = freeLanService.GetFreeLanServiceStatus();

                OnCommonStart();

                var started = freeLanService.StartFreeLan();
                if (started)
                {
                    SetStateValueLabel("Host - running.");
                    StartProcessCheck();
                    // record successful host start (host uses well-known 9.0.0.1 and port 12000)
                    try
                    {
                        userPrefs.SaveSuccessfulConnection("9.0.0.1", 12000);
                    }
                    catch
                    {
                        // non-fatal: ignore persistence failures
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex is InvalidInput or AlreadyRunning)
                {
                    ExceptionHelper.ShowMessageBox(ex);
                    return;
                }
                throw;
            }


        }
        private void OnClientStartClick(object sender, RoutedEventArgs e)
        {
            try
            {
                freeLanService.SetMode(FreeLanMode.CLIENT);
                freeLanService.SetPassphrase(TextBoxClientPassword.Text);
                freeLanService.SetHostIp(TextBoxClientHost.Text);
                freeLanService.SetClientId(TextBoxId.Text);
                freeLanService.SetRelayMode(CheckBoxClientRelay.IsChecked.GetValueOrDefault());
                freeLanService.SetShowShell(CheckBoxDebug.IsChecked.GetValueOrDefault());

                SaveClientDefaultsIfRequested();

                OnCommonStart();

                var started = freeLanService.StartFreeLan();
                if (started)
                {
                    SetStateValueLabel("Client - running.");
                    StartProcessCheck();
                    // record successful client connection
                    try
                    {
                        var host = TextBoxClientHost.Text ?? string.Empty;
                        userPrefs.SaveSuccessfulConnection(host, 12000);
                    }
                    catch
                    {
                        // non-fatal: ignore persistence failures
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex is InvalidInput or AlreadyRunning)
                {
                    ExceptionHelper.ShowMessageBox(ex);
                    return;
                }
                throw;
            }



        }

        private void OnFreeLanStopClick(object sender, RoutedEventArgs e)
        {
            OnFreeLanStop();
        }

        #endregion
        private void OnFreeLanStop()
        {
            SetStateValueLabel("Not running.");
            SetFreeLANAddressValueLabel("None");
            freeLanService.StopFreeLan();
            freeLanAddressCheck.Stop();
            foreach (WindowsService w in windowsServices.GetServicesToDisable())
            {
                w.Enable();
            }
            foreach (NetworkAdapter a in networkAdapters.GetAdaptersToDisable())
            {
                a.Enable();
            }
        }

        private void StartProcessCheck()
        {
            processCheck = new DispatcherTimer();
            processCheck.Tick += OnProcessCheck;
            processCheck.Interval = TimeSpan.FromSeconds(1.00);
            processCheck.Start();
        }

        private void StartFreeLANAddressCheck()
        {
            freeLanAddressCheck = new DispatcherTimer();
            freeLanAddressCheck.Tick += OnFreeLanAddressCheck;
            freeLanAddressCheck.Interval = TimeSpan.FromSeconds(3.00);
            freeLanAddressCheck.Start();
        }

        private void OnFreeLanAddressCheck(object? sender, EventArgs e)
        {
            _ = UpdateFreeLANAddress();

        }

        private void OnProcessCheck(object? sender, EventArgs e)
        {
            if (!freeLanService.IsRunning)
            {
                processCheck.Stop();
                OnFreeLanStop();
            }
        }

        private static bool TryGetIPv4(string input, out string ipv4)
        {
            ipv4 = string.Empty;
            if (string.IsNullOrWhiteSpace(input))
            {
                return false;
            }

            input = input.Trim();
            if (System.Net.IPAddress.TryParse(input, out System.Net.IPAddress? addr) && addr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                ipv4 = addr.ToString();
                return true;
            }
            return false;
        }

        private void OnPasteHostIpClick(object sender, RoutedEventArgs e)
        {
            if (Clipboard.ContainsText())
            {
                var txt = Clipboard.GetText();
                if (TryGetIPv4(txt, out var ip))
                {
                    TextBoxClientHost.Text = ip;
                }
                else
                {
                    _ = MessageBox.Show("Clipboard does not contain a valid IPv4 address.");
                }
            }
        }

        private void SaveClientDefaultsIfRequested()
        {
            if (CheckBoxRememberClient?.IsChecked != true)
            {
                return;
            }
            UserPreferencesService.ClientDefaults dto = new()
            {
                Host = TextBoxClientHost.Text ?? string.Empty,
                Password = TextBoxClientPassword.Text ?? string.Empty,
                Id = TextBoxId.Text ?? string.Empty,
                Relay = CheckBoxClientRelay.IsChecked == true,
                ShowDebug = CheckBoxDebug.IsChecked == true
            };
            userPrefs.SaveClient(dto);
        }

        // Add diagnostics button handler
        private void OnOpenDiagnostics(object sender, RoutedEventArgs e)
        {
            DiagnosticsWindow diag = new()
            {
                Owner = this
            };
            _ = diag.ShowDialog();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                processCheck.Stop();
                processCheck.Tick -= OnProcessCheck;

                freeLanAddressCheck.Stop();
                freeLanAddressCheck.Tick -= OnFreeLanAddressCheck;

                freeLanService.Dispose();
            }

            _disposed = true;
        }
    }
}
