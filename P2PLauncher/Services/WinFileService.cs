namespace P2PLauncher.Services
{
    internal sealed class WinFileService : IFileService
    {
        public bool CheckPath(string path, bool endsWithFile)
        {
            ArgumentNullException.ThrowIfNull(path);
            return endsWithFile ? System.IO.File.Exists(path) : System.IO.Directory.Exists(path);
        }
    }
}
