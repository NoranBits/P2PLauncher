namespace P2PLauncher.Services
{
    internal interface IFileService
    {
        bool CheckPath(string path, bool endsWithFile);
    }
}
