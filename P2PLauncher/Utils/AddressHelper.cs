using P2PLauncher.Model;

namespace P2PLauncher.Utils
{
    internal static class AddressHelper
    {
        public static AddressType GetAddressType(string input)
        {
            ArgumentNullException.ThrowIfNull(input);
            return input.Contains('.', StringComparison.Ordinal) ? AddressType.IPV4 : AddressType.UNKNOWN;
        }
    }
}
