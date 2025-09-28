using System.Diagnostics.CodeAnalysis;

namespace P2PLauncher.Exceptions
{
    [SuppressMessage("Naming", "CA1710:Identifiers should have correct suffix", Justification = "Historical name; changing would be breaking across UI and services.")]
    internal sealed class InvalidInput : Exception
    {
        public InvalidInput() { }
        public InvalidInput(string message) : base(message) { }
        public InvalidInput(string message, Exception innerException) : base(message, innerException) { }
    }
}
