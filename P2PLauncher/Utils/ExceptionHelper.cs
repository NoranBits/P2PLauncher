using System;
using System.Windows;

namespace P2PLauncher.Utils
{
    internal static class ExceptionHelper
    {
        public static void ShowMessageBox(Exception ex)
        {
            ArgumentNullException.ThrowIfNull(ex);
            MessageBox.Show($"The following error has occured:\n '{ex.Message}' ", "Something went wrong!", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
