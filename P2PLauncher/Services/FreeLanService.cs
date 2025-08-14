using P2PLauncher.Exceptions;
using P2PLauncher.Model;
using P2PLauncher.Utils;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;

namespace P2PLauncher.Services
{
    internal sealed class FreeLanService(WindowsServices windowsServices,
        FreeLanDetectionService freeLanDetectionService,
        IDialogService dialogService) : IDisposable
    {
        private readonly FreeLanDetectionService freeLanDetectionService = freeLanDetectionService;
        private readonly IDialogService dialogService = dialogService;
        private readonly WindowsServices windowsServices = windowsServices;

        private Process? process;

        private string passphrase = string.Empty;
        private string hostIp = string.Empty;
        private AddressType hostIpType;
        private string clientId = string.Empty;
        private FreeLanMode mode;
        private string relayMode = "no";
        private bool showShell;
        private StreamWriter? debugWrite;

        // New observable events for server telemetry
        public event Action? ServiceStarted;
        public event Action? ServiceStopped;
        public event Action<string>? OutputReceived;

        public void SetPassphrase(string content)
        {
            passphrase = content ?? string.Empty;
        }

        public void SetHostIp(string content)
        {
            hostIpType = AddressHelper.GetAddressType(content);
            switch (hostIpType)
            {
                case AddressType.UNKNOWN:
                    throw new InvalidInput("Invalid host/hub address!");
                case AddressType.IPV4:
                    hostIp = content!;
                    break;
                default:
                    break;
            }
        }

        public void SetClientId(string content)
        {
            if (!int.TryParse(content, out var parsed))
            {
                throw new InvalidInput("ID should be an number.");
            }

            if (parsed is < 2 or > 253)
            {
                throw new InvalidInput("ID should be in range between 2-253.");
            }

            clientId = content;
        }
        public void SetMode(FreeLanMode c)
        {
            mode = c;
        }

        public void SetRelayMode(bool c)
        {
            relayMode = c ? "yes" : "no";
        }

        public void SetShowShell(bool c)
        {
            showShell = c;
        }

        public bool IsThisValidIPForTheCurrentMode(string ip)
        {
            ArgumentNullException.ThrowIfNull(ip);
            return mode switch
            {
                FreeLanMode.CLIENT => ip.StartsWith("9.0.0", StringComparison.Ordinal) && !ip.Equals("9.0.0.1", StringComparison.Ordinal),
                FreeLanMode.CLIENT_HUB => ip.StartsWith("9.0.0", StringComparison.Ordinal) && !ip.Equals("9.0.0.1", StringComparison.Ordinal),
                FreeLanMode.HOST => ip.StartsWith("9.0.0", StringComparison.Ordinal),
                _ => false,
            };
        }

        public bool GetFreeLanServiceStatus()
        {
            WindowsService? freeLanService = windowsServices.GetServiceByName("FreeLAN Service");
            return freeLanService != null && freeLanService.Status == ServiceControllerStatus.Running;
        }

        public void SetFreeLanServiceStatus(bool start)
        {
            WindowsService? freeLanService = windowsServices.GetServiceByName("FreeLAN Service");
            if (freeLanService == null)
            {
                return;
            }

            if (start)
            {
                freeLanService.Enable();
            }
            else
            {
                freeLanService.Disable();
            }
        }

        public bool IsRunning => process != null && !process.HasExited;

        public void StopFreeLan()
        {
            try
            {
                if (process != null && !process.HasExited)
                {
                    process.Kill();
                }
            }
            finally
            {
                debugWrite?.Dispose();
                process?.Dispose();
                process = null;
                ServiceStopped?.Invoke();
            }
        }

        public static bool GetStrangeFreeLansRunning()
        {
            Process[] procs = Process.GetProcessesByName("freelan");
            return procs.Length > 0;
        }

        public static bool KillStrangeFreeLan()
        {
            var killed = false;
            Process[] procs = Process.GetProcessesByName("freelan");
            foreach (Process p in procs)
            {
                try
                {
                    p.Kill();
                    killed = true;
                }
                finally
                {
                    p.Dispose();
                }
            }
            return killed;
        }

        public bool StartFreeLan()
        {
            if (process != null && !process.HasExited)
            {
                throw new AlreadyRunning("Please stop it first.");
            }

            if (freeLanDetectionService.GetInstallationStatus() != FreeLanInstallationStatus.OK)
            {
                dialogService.ShowMessage("FreeLan is not configured! do it", "FreeLan missing.");
                return false;
            }

            if (GetFreeLanServiceStatus())
            {
                SetFreeLanServiceStatus(false);
            }

            if (GetStrangeFreeLansRunning())
            {
                _ = KillStrangeFreeLan();
            }

            if (!showShell)
            {
                debugWrite = new StreamWriter("debug.txt") { AutoFlush = true };
            }

            process = new Process();
            process.StartInfo.FileName = freeLanDetectionService.GetFreeLanExecutableLocation();

            switch (mode)
            {
                case FreeLanMode.CLIENT:
                    process.StartInfo.Arguments =
                        $"--security.passphrase {passphrase} --fscp.contact {hostIp}:12000 --switch.relay_mode_enabled {relayMode} --tap_adapter.ipv4_address_prefix_length 9.0.0.{clientId}/24 --tap_adapter.metric 1 --debug";
                    break;
                case FreeLanMode.HOST:
                    process.StartInfo.Arguments =
                        $"--security.passphrase {passphrase} --tap_adapter.ipv4_address_prefix_length 9.0.0.1/24 --switch.relay_mode_enabled {relayMode} --tap_adapter.metric 1 --debug";
                    break;
                case FreeLanMode.CLIENT_HUB:
                    process.StartInfo.Arguments =
                        $"--security.passphrase {passphrase} --fscp.contact {hostIp}:12000 --tap_adapter.dhcp_proxy_enabled no --tap_adapter.ipv4_dhcp true --tap_adapter.metric 1 --debug";
                    break;
                default:
                    break;
            }

            process.StartInfo.CreateNoWindow = !showShell;
            process.StartInfo.WindowStyle = showShell ? ProcessWindowStyle.Normal : ProcessWindowStyle.Hidden;

            if (!showShell)
            {
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.OutputDataReceived += (_, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        OutputReceived?.Invoke(e.Data);
                        debugWrite!.WriteLine(e.Data);
                    }
                };
            }

            _ = process.Start();
            if (!showShell)
            {
                process.BeginOutputReadLine();
            }

            var running = !process.HasExited;
            if (running)
            {
                ServiceStarted?.Invoke();
            }
            return running;
        }

        public void Dispose()
        {
            StopFreeLan();
            GC.SuppressFinalize(this);
        }
    }
}
