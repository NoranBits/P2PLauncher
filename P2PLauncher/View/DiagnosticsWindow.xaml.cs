using P2PLauncher.Services;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Windows;

namespace P2PLauncher.View
{
    internal partial class DiagnosticsWindow : Window
    {
        private readonly FeedbackService feedback = new();

        public DiagnosticsWindow()
        {
            InitializeComponent();
            TextBoxHost.Text = "127.0.0.1";
            TextBoxPort.Text = "12000";
        }

        private async void OnTestClick(object sender, RoutedEventArgs e)
        {
            TextBoxResults.Text = "Running tests...";
            var host = TextBoxHost.Text.Trim();
            if (!int.TryParse(TextBoxPort.Text, out var port))
            {
                port = 12000;
            }

            var results = await RunConnectivityChecks(host, port).ConfigureAwait(false);

            // Marshal back to UI thread to update controls
            Dispatcher.Invoke(() => { TextBoxResults.Text = string.Join("\n", results); });
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async Task<string[]> RunConnectivityChecks(string host, int port)
        {
            List<string> list = [];

            // TCP port check (fallback for UDP permission issues)
            try
            {
                using TcpClient client = new();
                Task connectTask = client.ConnectAsync(host, port);
                Task completed = await Task.WhenAny(connectTask, Task.Delay(3000)).ConfigureAwait(false);
                if (completed == connectTask && client.Connected)
                {
                    list.Add($"TCP {host}:{port} reachable");
                }
                else
                {
                    list.Add($"TCP {host}:{port} not reachable (timeout)");
                }
            }
            catch (SocketException ex)
            {
                list.Add($"TCP check socket error: {ex.Message}");
            }
            catch (ArgumentException ex)
            {
                list.Add($"TCP check argument error: {ex.Message}");
            }
            catch (ObjectDisposedException ex)
            {
                list.Add($"TCP check disposed error: {ex.Message}");
            }

            // Basic ICMP ping using System.Net.NetworkInformation.Ping
            try
            {
                using Ping ping = new();
                PingReply reply = await ping.SendPingAsync(host, 2000).ConfigureAwait(false);
                if (reply.Status == IPStatus.Success)
                {
                    list.Add($"ICMP ping: {reply.RoundtripTime} ms");
                }
                else
                {
                    list.Add($"ICMP ping failed: {reply.Status}");
                }
            }
            catch (PingException ex)
            {
                list.Add($"ICMP ping error: {ex.Message}");
            }
            catch (ArgumentNullException ex)
            {
                list.Add($"ICMP ping argument error: {ex.Message}");
            }
            catch (InvalidOperationException ex)
            {
                list.Add($"ICMP ping invalid operation: {ex.Message}");
            }

            // Log and return
            foreach (var l in list)
            {
                feedback.LogInfo(l);
            }

            return [.. list];
        }
    }
}
