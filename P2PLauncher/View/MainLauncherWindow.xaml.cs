using System.Diagnostics.CodeAnalysis;
using System;
using System.Windows;
using P2PLauncher.Model;
using P2PLauncher.ViewModels;

namespace P2PLauncher.View
{
    /// <summary>
    /// Interaction logic for MainLauncherWindow.xaml
    /// </summary>
    [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated via XAML")]
    internal sealed partial class MainLauncherWindow : Window, IWindow, IDisposable
    {
        private bool _disposed;

        public MainLauncherWindow()
        {
            InitializeComponent();
        }

        public void UpdateWindow()
        {
            // MVVM bindings handle state; no-op hook for legacy callers
        }

        private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            // Add any cleanup if needed
        }

        // Restored legacy handler: paste host/IP from clipboard into ViewModel
        private void OnPasteHostIpClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Clipboard.ContainsText())
                {
                    var text = Clipboard.GetText()?.Trim();
                    if (!string.IsNullOrEmpty(text) && DataContext is MainViewModel vm)
                    {
                        vm.Host = text;
                    }
                }
            }
            catch (System.Runtime.InteropServices.ExternalException)
            {
                // Clipboard unavailable or in use by another process
            }
            catch (ThreadStateException)
            {
                // Clipboard requires STA; ignore if not available
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            // free managed
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
