using System.Globalization;
using System.Windows;

namespace P2PLauncher
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    internal partial class App : Application
    {
        public App()
        {
            Current.DispatcherUnhandledException += OnDispatcherUnhandledException;
        }

        /// <summary>
        /// Catch any unhandled exception
        /// </summary>
        private static void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            var errorMsg = string.Format(CultureInfo.InvariantCulture, "An unhandled exception occurred: {0}", e.Exception);
            _ = MessageBox.Show(errorMsg, "Unhandled Exception!", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }
    }
}
