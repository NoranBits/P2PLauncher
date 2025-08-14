using P2PLauncher.Model;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Globalization;

namespace P2PLauncher.View
{
    /// <summary>
    /// Interaction logic for WindowsServicesWindow.xaml
    /// </summary>
    internal partial class WindowsServicesWindow : Window, IWindow
    {
        private readonly WindowsServices windowsServices;

        private readonly List<WindowsService> windowsServicesEnabled = new();
        private readonly List<WindowsService> windowsServicesDisabled = new();

        private readonly string[] servicesToInclude =
        {
            "VPN",
            "Radmin",
            "Hamachi",
            "Virtual Private Network"

        };
        private bool commonFilter;
        private string filterWith = string.Empty;

        public WindowsServicesWindow()
        {
            InitializeComponent();
            windowsServices = new WindowsServices();

            UpdateWindow();
        }

        private void MoveFromEnabledToDisabled(WindowsService item)
        {
            windowsServicesEnabled.Remove(item);
            windowsServicesDisabled.Add(item);
            UpdateAdaptersList();
        }
        private void MoveFromDisabledToEnabled(WindowsService item)
        {
            windowsServicesDisabled.Remove(item);
            windowsServicesEnabled.Add(item);
            UpdateAdaptersList();
        }

        private void OnMoveToDisabledButton(object sender, RoutedEventArgs e)
        {
            int indexToMove = ListBoxWindowsServicesOn.SelectedIndex;
            if (indexToMove == -1)
                return;

            var item = windowsServicesEnabled[indexToMove];
            MoveFromEnabledToDisabled(item);
        }
        private void OnMoveToEnabledButton(object sender, RoutedEventArgs e)
        {
            int indexToMove = ListBoxWindowsServicesOff.SelectedIndex;
            if (indexToMove == -1)
                return;

            var item = windowsServicesDisabled[indexToMove];
            MoveFromDisabledToEnabled(item);

        }

        private void UpdateAdaptersList()
        {
            ListBoxWindowsServicesOff.ItemsSource = new List<object>();
            ListBoxWindowsServicesOn.ItemsSource = new List<object>();
            ListBoxWindowsServicesOn.ItemsSource = windowsServicesEnabled;
            ListBoxWindowsServicesOff.ItemsSource = windowsServicesDisabled;

            windowsServices.SaveServicesToDisable(windowsServicesDisabled);

        }

        public void UpdateWindow()
        {
            var services = windowsServices.GetServices();
            var servicesToDisable = windowsServices.GetServiceNamesToDisable();

            windowsServicesEnabled.Clear();
            windowsServicesDisabled.Clear();

            foreach (var service in services)
            {

                if (servicesToDisable.Contains(service.Name))
                {
                    windowsServicesDisabled.Add(service);
                }
                else
                {
                    if (!string.IsNullOrEmpty(filterWith))
                    {
                        if (!service.ToString().Contains(filterWith, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                    }
                    bool addToTheList = !commonFilter;
                    foreach (string filter in servicesToInclude)
                    {
                        if (service.ToString().Contains(filter, StringComparison.Ordinal))
                        {
                            addToTheList = true;
                        }

                    }

                    if (addToTheList)
                    {
                        windowsServicesEnabled.Add(service);
                    }
                }
            }
            UpdateAdaptersList();


        }

        private void TextBoxSearch_KeyUp(object sender, KeyEventArgs e)
        {
            filterWith = TextBoxSearch.Text ?? string.Empty;
            UpdateWindow();
        }

        private void CheckBoxFilter_Checked(object sender, RoutedEventArgs e)
        {
            commonFilter = true;
            UpdateWindow();
        }

        private void CheckBoxFilter_Unchecked(object sender, RoutedEventArgs e)
        {
            commonFilter = false;
            UpdateWindow();
        }
    }
}
