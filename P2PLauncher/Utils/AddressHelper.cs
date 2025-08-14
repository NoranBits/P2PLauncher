using P2PLauncher.Model;
using System;

namespace P2PLauncher.Utils
{
    internal static class AddressHelper
    {
        public static AddressType GetAddressType(string input)
        {
            ArgumentNullException.ThrowIfNull(input);
            if (input.Contains('.', StringComparison.Ordinal))
                return AddressType.IPV4;
            return AddressType.UNKNOWN;
        }
    }
}
