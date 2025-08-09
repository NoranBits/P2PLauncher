using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace P2PLauncher.Model
{
    public class WindowsService
    {
        public string DisplayName { get; set; }
        public string Name { get; set; }
        public ServiceType Type { get; set; }
        public ServiceControllerStatus Status { get; set; }

        public WindowsService FromServiceController(ServiceController serviceController)
        {
            this.DisplayName = serviceController.DisplayName;
            this.Name = serviceController.ServiceName;
            this.Type = serviceController.ServiceType;
            this.Status = serviceController.Status;

            return this;
        }

        public override string ToString()
        {
            return $"{DisplayName} - {Name} - {Type} - {Status}";
        }

        public void Enable()
        {
            try
            {
                using (var sc = new ServiceController(Name))
                {
                    if (sc.Status != ServiceControllerStatus.Running && sc.Status != ServiceControllerStatus.StartPending)
                    {
                        sc.Start();
                        sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(10));
                    }
                }
                return;
            }
            catch
            {
                // Fallback to net command
            }

            var psi = new ProcessStartInfo("net", "start \"" + Name + "\" /y")
            {
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using (var p = new Process { StartInfo = psi })
            {
                p.Start();
                p.WaitForExit(10000);
            }
        }
        public void Disable()
        {
            try
            {
                using (var sc = new ServiceController(Name))
                {
                    if (sc.Status != ServiceControllerStatus.Stopped && sc.Status != ServiceControllerStatus.StopPending)
                    {
                        sc.Stop();
                        sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10));
                    }
                }
                return;
            }
            catch
            {
                // Fallback to net command
            }

            var psi = new ProcessStartInfo("net", "stop \"" + Name + "\" /y")
            {
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using (var p = new Process { StartInfo = psi })
            {
                p.Start();
                p.WaitForExit(10000);
            }
        }


    }
}
