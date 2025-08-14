using System.Windows;
using System.Globalization;

namespace P2PLauncher.Utils
{
    internal static class ExceptionHelper
    {
        public static void ShowMessageBox(Exception ex)
        {
            ArgumentNullException.ThrowIfNull(ex);
            // Format using InvariantCulture and avoid string interpolation culture ambiguity
            var message = string.Format(CultureInfo.InvariantCulture, "The following error has occurred:\n '{0}' ", ex.Message);
            _ = MessageBox.Show(message, "Something went wrong!", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
