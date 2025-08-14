using Microsoft.Win32;
using System.Windows;

namespace P2PLauncher.Services
{
    internal class WinDialogService : IDialogService
    {
        public string FilePath { get; set; } = string.Empty;

        public bool OpenFileDialog()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                FilePath = openFileDialog.FileName;
                return true;
            }
            return false;
        }

        public void ShowMessage(string message, string title)
        {
            MessageBox.Show(message, title);
        }
    }
}
