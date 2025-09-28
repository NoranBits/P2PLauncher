using System.Diagnostics.CodeAnalysis;
using P2PLauncher.Model;
using P2PLauncher.Services;
using P2PLauncher.Utils;
using System.Diagnostics;
using System.Windows;

namespace P2PLauncher.View
{
    /// <summary>
    /// Interaction logic for FreeLanDetectionWindow.xaml
    /// </summary>
    [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated via XAML")]
    internal sealed partial class FreeLanDetectionWindow : Window, IWindow
    {
        private readonly FreeLanDetectionService freeLanDetectionService;
        private readonly IFileService fileService;
        private readonly IDialogService dialogService;
        private bool FreeLanAutoDetectFailed;
        private FreeLanInstallationStatus FreeLanInstallationStatus;

        private void SetStatus(string statusContent)
        {
            LabelStatus.Content = $"Status: {statusContent}";

        }

        private void SetFindFreeLanButtonVisibility(bool visible)
        {
            ButtonFindFreeLan.Visibility = visible ? Visibility.Visible : Visibility.Hidden;
        }
        private void SetFreeLanLocationTabControlVisibility(bool visible)
        {
            TabControlFreeLanLocation.Visibility = visible ? Visibility.Visible : Visibility.Hidden;
        }
        private void OnFindFreeLanButton(object sender, RoutedEventArgs e)
        {
            var result = freeLanDetectionService.FindFreeLan();
            FreeLanAutoDetectFailed = !result;
            UpdateWindow();
            _ = result
                ? MessageBox.Show("FreeLan found! You can close this window now!", "Located!")
                : MessageBox.Show("You need to find FreeLan by locating it manually!", "Could not locate.");
        }
        private void OnSelectFreeLanPathButton(object sender, RoutedEventArgs e)
        {
            var result = freeLanDetectionService.SelectPath();
            UpdateWindow();
            if (!result)
            {
                _ = MessageBox.Show("Something went wrong while selecting file!", "Try again!");
            }
        }
        private void OnDownloadFreeLanButton(object sender, RoutedEventArgs e)
        {
            _ = Process.Start(freeLanDetectionService.GetDownloadUrl());
        }

        private void SetDownloadFreeLanHintLabel(string content)
        {
            LabelDownloadFreeLanHint.Content = content;
        }




        public FreeLanDetectionWindow()
        {
            InitializeComponent();

            fileService = new WinFileService();
            dialogService = new WinDialogService();
            freeLanDetectionService = new FreeLanDetectionService(fileService, dialogService);

            UpdateWindow();

        }

        public void UpdateWindow()
        {
            FreeLanInstallationStatus currentStatus = freeLanDetectionService.GetInstallationStatus();
            SetStatus(EnumHelper.GetDescription(currentStatus));
            SetFreeLanLocationTabControlVisibility(FreeLanAutoDetectFailed);
            SetDownloadFreeLanHintLabel(EnvHelper.Is64Bit() ? "You need to download x64 version" :
                "You need to download x86 (32-bit) version.");

            if (currentStatus != FreeLanInstallationStatus)
            {
                UpdateWindowAcknowledgeChange(currentStatus);
            }

            switch (currentStatus)
            {
                case FreeLanInstallationStatus.OK:
                    SetFindFreeLanButtonVisibility(false);
                    SetFreeLanLocationTabControlVisibility(false);
                    break;
                case FreeLanInstallationStatus.UNK:
                    break;
                case FreeLanInstallationStatus.CONFIG_NOT_SET:
                    break;
                case FreeLanInstallationStatus.INVALID_PATH:
                    break;
                default:
                    break;
            }

            FreeLanInstallationStatus = currentStatus;

        }

        private static void UpdateWindowAcknowledgeChange(FreeLanInstallationStatus newStatus)
        {
            switch (newStatus)
            {
                case FreeLanInstallationStatus.OK:
                    _ = MessageBox.Show("FreeLan found! You can close this window now.", "Ok!");
                    break;
                case FreeLanInstallationStatus.UNK:
                    break;
                case FreeLanInstallationStatus.CONFIG_NOT_SET:
                    break;
                case FreeLanInstallationStatus.INVALID_PATH:
                    break;
                default:
                    break;
            }

        }



    }
}
