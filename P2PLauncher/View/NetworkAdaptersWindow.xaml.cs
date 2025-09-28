using System.Diagnostics.CodeAnalysis;
using P2PLauncher.Model;
using System.Windows;

namespace P2PLauncher.View
{
    /// <summary>
    /// Interaction logic for NetworkAdaptersWindow.xaml
    /// </summary>
    [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated via XAML")]
    internal sealed partial class NetworkAdaptersWindow : Window, IWindow
    {
        private readonly NetworkAdapters networkAdapters;

        private readonly List<NetworkAdapter> NetworkAdaptersEnabled = [];
        private readonly List<NetworkAdapter> NetworkAdaptersDisabled = [];

        private void MoveFromEnabledToDisabled(NetworkAdapter item)
        {
            _ = NetworkAdaptersEnabled.Remove(item);
            NetworkAdaptersDisabled.Add(item);
            UpdateAdaptersList();
        }
        private void MoveFromDisabledToEnabled(NetworkAdapter item)
        {
            _ = NetworkAdaptersDisabled.Remove(item);
            NetworkAdaptersEnabled.Add(item);
            UpdateAdaptersList();
        }


        public NetworkAdaptersWindow()
        {
            InitializeComponent();

            networkAdapters = new NetworkAdapters();

            UpdateWindow();


        }

        private void OnMoveToDisabledButton(object sender, RoutedEventArgs e)
        {
            var indexToMove = ListBoxNetworkAdaptersOn.SelectedIndex;
            if (indexToMove == -1)
            {
                return;
            }

            NetworkAdapter item = NetworkAdaptersEnabled[indexToMove];
            MoveFromEnabledToDisabled(item);
        }
        private void OnMoveToEnabledButton(object sender, RoutedEventArgs e)
        {
            var indexToMove = ListBoxNetworkAdaptersOff.SelectedIndex;
            if (indexToMove == -1)
            {
                return;
            }

            NetworkAdapter item = NetworkAdaptersDisabled[indexToMove];
            MoveFromDisabledToEnabled(item);

        }

        private void UpdateAdaptersList()
        {
            ListBoxNetworkAdaptersOff.ItemsSource = new List<object>();
            ListBoxNetworkAdaptersOn.ItemsSource = new List<object>();
            ListBoxNetworkAdaptersOn.ItemsSource = NetworkAdaptersEnabled;
            ListBoxNetworkAdaptersOff.ItemsSource = NetworkAdaptersDisabled;

            NetworkAdapters.SaveAdaptersToDisable(NetworkAdaptersDisabled);

        }

        public void UpdateWindow()
        {
            IReadOnlyList<NetworkAdapter> adapters = networkAdapters.GetNetworkAdapters();
            var adaptersToDisable = NetworkAdapters.GetAdapterNamesToDisable();

            NetworkAdaptersEnabled.Clear();
            NetworkAdaptersDisabled.Clear();

            foreach (NetworkAdapter adapter in adapters)
            {
                if (adaptersToDisable.Contains(adapter.Name))
                {
                    NetworkAdaptersDisabled.Add(adapter);
                }
                else
                {
                    NetworkAdaptersEnabled.Add(adapter);
                }
            }
            UpdateAdaptersList();


        }
    }
}
