using P2PLauncher.Model;
using System;
using System.Collections.Generic;
using System.Windows;

namespace P2PLauncher.View
{
    /// <summary>
    /// Interaction logic for NetworkAdaptersWindow.xaml
    /// </summary>
    internal partial class NetworkAdaptersWindow : Window, IWindow
    {
        private readonly NetworkAdapters networkAdapters;

        private readonly List<NetworkAdapter> NetworkAdaptersEnabled = new List<NetworkAdapter>();
        private readonly List<NetworkAdapter> NetworkAdaptersDisabled = new List<NetworkAdapter>();

        void MoveFromEnabledToDisabled(NetworkAdapter item)
        {
            NetworkAdaptersEnabled.Remove(item);
            NetworkAdaptersDisabled.Add(item);
            UpdateAdaptersList();
        }
        void MoveFromDisabledToEnabled(NetworkAdapter item)
        {
            NetworkAdaptersDisabled.Remove(item);
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
            int indexToMove = ListBoxNetworkAdaptersOn.SelectedIndex;
            if (indexToMove == -1)
                return;

            NetworkAdapter item = NetworkAdaptersEnabled[indexToMove];
            MoveFromEnabledToDisabled(item);
        }
        private void OnMoveToEnabledButton(object sender, RoutedEventArgs e)
        {
            int indexToMove = ListBoxNetworkAdaptersOff.SelectedIndex;
            if (indexToMove == -1)
                return;

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
            List<NetworkAdapter> adapters = networkAdapters.GetNetworkAdapters();
            string[] adaptersToDisable = NetworkAdapters.GetAdapterNamesToDisable();
            
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
