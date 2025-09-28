using System.Diagnostics.CodeAnalysis;

namespace P2PLauncher.Model
{
    [SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Existing persisted values and string comparisons rely on these names; renaming would be breaking.")]
    internal enum FreeLanMode
    {
        HOST,
        CLIENT,
        CLIENT_HUB
    }
}
