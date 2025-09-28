namespace P2PLauncher.Services
{
    internal interface IDialogService
    {
        void ShowMessage(string message, string title);
        string FilePath { get; set; }
        bool OpenFileDialog();
    }
}
