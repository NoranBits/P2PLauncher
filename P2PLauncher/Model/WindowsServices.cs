using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.ServiceProcess;

namespace P2PLauncher.Model
{
    internal class WindowsServices
    {
        [SuppressMessage("Performance", "CA1822:Mark members as static")]
        public List<WindowsService> GetServices()
        {
            var windowsServices = new List<WindowsService>();

            ServiceController[] services = ServiceController.GetServices();

            foreach (ServiceController service in services)
            {
                windowsServices.Add(new WindowsService().FromServiceController(service));
            }
            return windowsServices;
        }

        [SuppressMessage("Performance", "CA1822:Mark members as static")]
        public WindowsService? GetServiceByName(string serviceName)
        {
            var controller = ServiceController.GetServices()
                .FirstOrDefault(sc => sc.ServiceName.Equals(serviceName, StringComparison.Ordinal));
            if (controller == null)
            {
                return null;
            }
            return new WindowsService().FromServiceController(controller);
        }

        public List<WindowsService> GetServicesWithType(ServiceType serviceType)
        {
            var withType = new List<WindowsService>();

            foreach (WindowsService service in GetServices())
            {
                if (service.Type == serviceType)
                {
                    withType.Add(service);
                }
            }
            return withType;
        }

        public List<WindowsService> GetServicesToDisable()
        {
            string[] toDisable = GetServiceNamesToDisable();
            var toDisableList = new List<WindowsService>();
            foreach (WindowsService w in GetServices())
            {
                if (toDisable.Contains(w.Name, StringComparer.Ordinal))
                {
                    toDisableList.Add(w);
                }
            }
            return toDisableList;

        }

        [SuppressMessage("Performance", "CA1822:Mark members as static")]
        public void SaveServicesToDisable(List<WindowsService> services)
        {
            string toSave = string.Join(",", services.Select(s => s.Name));
            Properties.Settings.Default.ServicesToDisable = toSave;
            Properties.Settings.Default.Save();
            Properties.Settings.Default.Upgrade();
            Properties.Settings.Default.Reload();
        }

        [SuppressMessage("Performance", "CA1822:Mark members as static")]
        public string[] GetServiceNamesToDisable()
        {
            string saved = Properties.Settings.Default.ServicesToDisable;
            if (string.IsNullOrEmpty(saved))
            {
                return Array.Empty<string>();
            }
            return saved.Split(',');
        }

    }
}
