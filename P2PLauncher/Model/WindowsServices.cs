using System.Diagnostics.CodeAnalysis;
using System.ServiceProcess;

namespace P2PLauncher.Model
{
    internal sealed class WindowsServices
    {
        [SuppressMessage("Performance", "CA1822:Mark members as static")]
        public List<WindowsService> GetServices()
        {
            List<WindowsService> windowsServices = [];

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
            ServiceController? controller = ServiceController.GetServices()
                .FirstOrDefault(sc => sc.ServiceName.Equals(serviceName, StringComparison.Ordinal));
            return controller == null ? null : new WindowsService().FromServiceController(controller);
        }

        public List<WindowsService> GetServicesWithType(ServiceType serviceType)
        {
            List<WindowsService> withType = [];

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
            var toDisable = GetServiceNamesToDisable();
            List<WindowsService> toDisableList = [];
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
            var toSave = string.Join(",", services.Select(s => s.Name));
            Properties.Settings.Default.ServicesToDisable = toSave;
            Properties.Settings.Default.Save();
            Properties.Settings.Default.Upgrade();
            Properties.Settings.Default.Reload();
        }

        [SuppressMessage("Performance", "CA1822:Mark members as static")]
        public string[] GetServiceNamesToDisable()
        {
            var saved = Properties.Settings.Default.ServicesToDisable;
            return string.IsNullOrEmpty(saved) ? [] : saved.Split(',');
        }

    }
}
