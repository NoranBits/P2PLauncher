using System.IO;

namespace P2PLauncher.Services
{
    internal sealed class WinFileService : IFileService
    {
        public bool CheckPath(string path, bool endsWithFile)
        {
            ArgumentNullException.ThrowIfNull(path);
            return endsWithFile ? File.Exists(path) : Directory.Exists(path);
        }
    }
}
