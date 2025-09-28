using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Globalization;

namespace P2PLauncher.Services
{
    internal sealed class ConnectionTestService
    {
        internal sealed class Result
        {
            public bool PingSuccess { get; set; }
            public long PingRoundtripMs { get; set; }
            public bool TcpPortOpen { get; set; }
            public int Port { get; set; }
            public string Summary => string.Format(CultureInfo.InvariantCulture, "Ping: {0}, Port {1}: {2}", PingSuccess ? $"OK ({PingRoundtripMs}ms)" : "Fail", Port, TcpPortOpen ? "Open" : "Closed/Timeout");
        }

        public static async Task<Result> TestAllAsync(string host, int port = 12000, int timeoutMs = 1000)
        {
            if (string.IsNullOrWhiteSpace(host))
            {
                throw new ArgumentNullException(nameof(host));
            }

            Result result = new() { Port = port };

            // Ping
            try
            {
                using Ping ping = new();
                PingReply reply = await ping.SendPingAsync(host, timeoutMs).ConfigureAwait(false);
                if (reply != null && reply.Status == IPStatus.Success)
                {
                    result.PingSuccess = true;
                    result.PingRoundtripMs = reply.RoundtripTime;
                }
                else
                {
                    result.PingSuccess = false;
                    result.PingRoundtripMs = -1;
                }
            }
            catch (PingException)
            {
                // Known ping failure
                result.PingSuccess = false;
                result.PingRoundtripMs = -1;
            }

            // TCP port check (attempt connect with timeout)
            try
            {
                using TcpClient tcp = new();
                Task connectTask = tcp.ConnectAsync(host, port);
                Task completed = await Task.WhenAny(connectTask, Task.Delay(timeoutMs)).ConfigureAwait(false);
                result.TcpPortOpen = completed == connectTask && tcp.Connected;
            }
            catch (SocketException)
            {
                // Port closed or unreachable
                result.TcpPortOpen = false;
            }
            catch (ArgumentException)
            {
                // Invalid host/port
                result.TcpPortOpen = false;
            }
            catch (InvalidOperationException)
            {
                result.TcpPortOpen = false;
            }

            return result;
        }
    }
}
