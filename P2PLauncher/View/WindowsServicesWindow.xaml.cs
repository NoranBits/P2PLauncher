using P2PLauncher.Model;
using System.Windows;
using System.Windows.Input;

namespace P2PLauncher.View
{
    /// <summary>
    /// Interaction logic for WindowsServicesWindow.xaml
    /// </summary>
    internal partial class WindowsServicesWindow : Window, IWindow
    {
        private readonly WindowsServices windowsServices;

        private readonly List<WindowsService> windowsServicesEnabled = [];
        private readonly List<WindowsService> windowsServicesDisabled = [];

        private readonly string[] servicesToInclude =
        [
            "VPN",
            "Radmin",
            "Hamachi",
            "Virtual Private Network"

        ];
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
            _ = windowsServicesEnabled.Remove(item);
            windowsServicesDisabled.Add(item);
            UpdateAdaptersList();
        }
        private void MoveFromDisabledToEnabled(WindowsService item)
        {
            _ = windowsServicesDisabled.Remove(item);
            windowsServicesEnabled.Add(item);
            UpdateAdaptersList();
        }

        private void OnMoveToDisabledButton(object sender, RoutedEventArgs e)
        {
            var indexToMove = ListBoxWindowsServicesOn.SelectedIndex;
            if (indexToMove == -1)
            {
                return;
            }

            WindowsService item = windowsServicesEnabled[indexToMove];
            MoveFromEnabledToDisabled(item);
        }
        private void OnMoveToEnabledButton(object sender, RoutedEventArgs e)
        {
            var indexToMove = ListBoxWindowsServicesOff.SelectedIndex;
            if (indexToMove == -1)
            {
                return;
            }

            WindowsService item = windowsServicesDisabled[indexToMove];
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
            List<WindowsService> services = windowsServices.GetServices();
            var servicesToDisable = windowsServices.GetServiceNamesToDisable();

            windowsServicesEnabled.Clear();
            windowsServicesDisabled.Clear();

            foreach (WindowsService service in services)
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
                    var addToTheList = !commonFilter;
                    foreach (var filter in servicesToInclude)
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
