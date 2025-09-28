using System.Diagnostics.CodeAnalysis;

namespace P2PLauncher.Exceptions
{
    [SuppressMessage("Naming", "CA1710:Identifiers should have correct suffix", Justification = "Historical public contract across app; renaming would be breaking.")]
    internal sealed class AlreadyRunning : Exception
    {
        public AlreadyRunning() { }
        public AlreadyRunning(string message) : base(message) { }
        public AlreadyRunning(string message, Exception innerException) : base(message, innerException) { }
    }
}
