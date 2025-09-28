using Microsoft.Win32;
using System.Windows;

namespace P2PLauncher.Services
{
    internal sealed class WinDialogService : IDialogService
    {
        public string FilePath { get; set; } = string.Empty;

        public bool OpenFileDialog()
        {
            OpenFileDialog openFileDialog = new();
            var result = openFileDialog.ShowDialog();
            if (result == true)
            {
                FilePath = openFileDialog.FileName;
                return true;
            }

            return false;
        }

        public void ShowMessage(string message, string title)
        {
            // Use MessageBoxResult to satisfy analyzer that the return value was considered
            _ = MessageBox.Show(message, title);
        }
    }
}
