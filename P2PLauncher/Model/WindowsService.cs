using System;
using System.ComponentModel;
using System.Diagnostics;
using System.ServiceProcess;

namespace P2PLauncher.Model
{
    internal class WindowsService
    {
        public string DisplayName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public ServiceType Type { get; set; }
        public ServiceControllerStatus Status { get; set; }

        public WindowsService FromServiceController(ServiceController serviceController)
        {
            ArgumentNullException.ThrowIfNull(serviceController);
            DisplayName = serviceController.DisplayName;
            Name = serviceController.ServiceName;
            Type = serviceController.ServiceType;
            Status = serviceController.Status;
            return this;
        }

        public override string ToString() => $"{DisplayName} - {Name} - {Type} - {Status}";

        public void Enable()
        {
            try
            {
                using var sc = new ServiceController(Name);
                if (sc.Status == ServiceControllerStatus.Running)
                    return;
                sc.Start();
                sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(15));
            }
            catch (InvalidOperationException)
            {
                // Fallback to net start if ServiceController fails (permissions, etc.)
                var psi = new ProcessStartInfo("net", $"start \"{Name}\" /y")
                {
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                using Process? p = Process.Start(psi);
            }
            catch (Win32Exception)
            {
                var psi = new ProcessStartInfo("net", $"start \"{Name}\" /y")
                {
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                using Process? p = Process.Start(psi);
            }
            catch (System.ServiceProcess.TimeoutException)
            {
                // ignore timeout; state might still be transitioning
            }
        }

        public void Disable()
        {
            try
            {
                using var sc = new ServiceController(Name);
                if (sc.Status == ServiceControllerStatus.Stopped)
                    return;
                sc.Stop();
                sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(15));
            }
            catch (InvalidOperationException)
            {
                var psi = new ProcessStartInfo("net", $"stop \"{Name}\" /y")
                {
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                using Process? p = Process.Start(psi);
            }
            catch (Win32Exception)
            {
                var psi = new ProcessStartInfo("net", $"stop \"{Name}\" /y")
                {
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                using Process? p = Process.Start(psi);
            }
            catch (System.ServiceProcess.TimeoutException)
            {
                // ignore timeout; state might still be transitioning
            }
        }
    }
}
