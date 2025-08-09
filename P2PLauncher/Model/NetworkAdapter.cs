using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace P2PLauncher.Model
{
    public class NetworkAdapter
    {
        public string Description { get; set; }
        public string ID { get; set; }
        public string Manufacturer { get; set; }
        public string Name { get; set; }
        public string ConnectionId { get; set; }
        public bool Enabled { get; set; }
        public string ProductName { get; set; }
        public string ServiceName { get; set; }



        /// <summary>
        /// Generates NetworkAdapter object based on WMI data
        /// </summary>
        /// <param name="managementBaseObject">WMI object</param>
        /// <returns>Filled in NetworkAdapter object.</returns>
        public NetworkAdapter FromWMI(ManagementBaseObject managementBaseObject)
        {
            this.Description = (string)managementBaseObject["Description"];
            this.ID = (string)managementBaseObject["DeviceID"];
            this.Manufacturer = (string)managementBaseObject["Manufacturer"];
            this.Name = (string)managementBaseObject["Name"];
            this.ConnectionId = (string)managementBaseObject["NetConnectionID"];
            //this.Enabled = (bool)managementBaseObject["NetEnabled"];
            this.ProductName = (string)managementBaseObject["ProductName"];
            this.ServiceName = (string)managementBaseObject["ServiceName"];
            return this;
        }

        /// <summary>
        /// Returns representation of this object in string.
        /// </summary>
        /// <returns>String containing all data stored in this object.</returns>
        public override string ToString()
        {
            return $"{Name}";
        }

        public void Enable()
        {
            // Try WMI first
            try
            {
                var query = new SelectQuery($"SELECT * FROM Win32_NetworkAdapter WHERE DeviceID='{ID}'");
                using (var searcher = new ManagementObjectSearcher(query))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        obj.InvokeMethod("Enable", null);
                        return;
                    }
                }
            }
            catch
            {
                // Fallback to netsh
            }

            // Fallback: netsh (requires admin)
            var name = string.IsNullOrWhiteSpace(ConnectionId) ? Name : ConnectionId;
            if (string.IsNullOrWhiteSpace(name)) return;

            var psi = new ProcessStartInfo("netsh", "interface set interface name=\"" + name + "\" admin=enabled")
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
                p.WaitForExit(5000);
            }
        }
        public void Disable()
        {
            // Try WMI first
            try
            {
                var query = new SelectQuery($"SELECT * FROM Win32_NetworkAdapter WHERE DeviceID='{ID}'");
                using (var searcher = new ManagementObjectSearcher(query))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        obj.InvokeMethod("Disable", null);
                        return;
                    }
                }
            }
            catch
            {
                // Fallback to netsh
            }

            var name = string.IsNullOrWhiteSpace(ConnectionId) ? Name : ConnectionId;
            if (string.IsNullOrWhiteSpace(name)) return;

            var psi = new ProcessStartInfo("netsh", "interface set interface name=\"" + name + "\" admin=disabled")
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
                p.WaitForExit(5000);
            }
        }
        
    }
}
