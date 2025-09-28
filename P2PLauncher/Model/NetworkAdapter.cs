using System.Diagnostics;
using System.Management;
using System.Diagnostics.CodeAnalysis;

namespace P2PLauncher.Model
{
    [SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Used by public NetworkAdapters API and UI bindings.")]
    public class NetworkAdapter
    {
        public string Description { get; set; } = string.Empty;
        public string ID { get; set; } = string.Empty;
        public string Manufacturer { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ConnectionId { get; set; } = string.Empty;
        public bool Enabled { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;

        /// <summary>
        /// Generates NetworkAdapter object based on WMI data
        /// </summary>
        /// <param name="managementBaseObject">WMI object</param>
        /// <returns>Filled in NetworkAdapter object.</returns>
        public NetworkAdapter FromWMI(ManagementBaseObject managementBaseObject)
        {
            ArgumentNullException.ThrowIfNull(managementBaseObject);

            Description = managementBaseObject[nameof(Description)] as string ?? string.Empty;
            ID = managementBaseObject["DeviceID"] as string ?? string.Empty;
            Manufacturer = managementBaseObject[nameof(Manufacturer)] as string ?? string.Empty;
            Name = managementBaseObject[nameof(Name)] as string ?? string.Empty;
            ConnectionId = managementBaseObject["NetConnectionID"] as string ?? string.Empty;
            // Enabled could be null in some adapters; keep default when not present
            // var enabled = managementBaseObject["NetEnabled"] as bool?;
            ProductName = managementBaseObject[nameof(ProductName)] as string ?? string.Empty;
            ServiceName = managementBaseObject[nameof(ServiceName)] as string ?? string.Empty;
            return this;
        }

        public override string ToString()
        {
            return Name;
        }

        public void Enable()
        {
            var psi = new ProcessStartInfo("netsh", $"interface set interface \"{ConnectionId}\" enable")
            {
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            using var p = Process.Start(psi);
        }

        public void Disable()
        {
            var psi = new ProcessStartInfo("netsh", $"interface set interface \"{ConnectionId}\" disable")
            {
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            using var p = Process.Start(psi);
        }
    }
}
