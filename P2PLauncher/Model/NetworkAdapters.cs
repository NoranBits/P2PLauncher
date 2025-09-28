using System.Management;
using System.Net.NetworkInformation;

namespace P2PLauncher.Model
{
    internal sealed class NetworkAdapters
    {
        /// <summary>
        /// Adapters containing the specific string should be ignored.
        /// WAN, Kernel, Bluetooth.
        /// </summary>
        private readonly string[] adaptersToIgnore =
        [
            "WAN",
            "Kernel",
            "Bluetooth"
        ];

        public static IReadOnlyList<string> GetTAPCurrentIP()
        {
            List<string> possibleAddresses = [];
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.Description.Contains("TAP-Windows Adapter V9", StringComparison.Ordinal))
                {
                    foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        {
                            possibleAddresses.Add(ip.Address.ToString());
                        }
                    }
                }
            }
            return possibleAddresses;
        }

        /// <summary>
        /// Detects all network adapters installed in the system
        /// (software and hardware)
        /// </summary>
        /// <returns>List of network adapters</returns>
        public IReadOnlyList<NetworkAdapter> GetNetworkAdapters(string extraQueryContent = "")
        {
            List<NetworkAdapter> networkAdapters = [];
            ObjectQuery query = new($"SELECT * FROM Win32_NetworkAdapter {extraQueryContent}");
            using ManagementObjectSearcher searcher = new(query);
            ManagementObjectCollection queryCollection = searcher.Get();
            foreach (ManagementBaseObject m in queryCollection)
            {
                NetworkAdapter adapter = new NetworkAdapter().FromWMI(m);
                var add = true;
                foreach (var ignoreWord in adaptersToIgnore)
                {
                    if (adapter.ToString().Contains(ignoreWord, StringComparison.Ordinal))
                    {
                        add = false;
                    }
                }
                if (add)
                {
                    networkAdapters.Add(adapter);
                }
            }
            return networkAdapters;
        }

        public static void SaveAdaptersToDisable(IReadOnlyList<NetworkAdapter> adapters)
        {
            ArgumentNullException.ThrowIfNull(adapters);
            var toSave = string.Empty;
            for (var i = 0; i < adapters.Count; i++)
            {
                toSave += adapters[i].Name;
                if (i != adapters.Count - 1)
                {
                    toSave += ",";
                }
            }
            Properties.Settings.Default.AdaptersToDisable = toSave;
            Properties.Settings.Default.Save();
            Properties.Settings.Default.Upgrade();
            Properties.Settings.Default.Reload();
        }

        public static string[] GetAdapterNamesToDisable()
        {
            var saved = Properties.Settings.Default.AdaptersToDisable;
            return string.IsNullOrEmpty(saved) ? [] : saved.Split(',');
        }
        public IReadOnlyList<NetworkAdapter> GetAdaptersToDisable()
        {
            var toDisable = GetAdapterNamesToDisable();
            List<NetworkAdapter> toDisableList = [];
            foreach (NetworkAdapter w in GetNetworkAdapters())
            {
                if (toDisable.Contains(w.Name))
                {
                    toDisableList.Add(w);
                }
            }
            return toDisableList;

        }
    }
}
