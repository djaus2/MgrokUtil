using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;

namespace NgrokTunnelsConfig;

internal static class NetworkValidator
{
    public static bool TryValidateLocalNetwork(string networkText, out string? error)
    {
        error = null;

        if (!IPAddress.TryParse(networkText, out var ip) || ip.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
        {
            error = $"Invalid IPv4 address: {networkText}";
            return false;
        }

        var ipBytes = ip.GetAddressBytes();

        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            IPInterfaceProperties? props;
            try
            {
                props = ni.GetIPProperties();
            }
            catch
            {
                continue;
            }

            foreach (var uni in props.UnicastAddresses)
            {
                if (uni.Address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    continue;
                }

                var addrBytes = uni.Address.GetAddressBytes();
                var maskBytes = uni.IPv4Mask?.GetAddressBytes();
                if (maskBytes is null || maskBytes.Length != 4)
                {
                    continue;
                }

                var networkBytes = new byte[4];
                for (var i = 0; i < 4; i++)
                {
                    networkBytes[i] = (byte)(addrBytes[i] & maskBytes[i]);
                }

                var localNetwork = new IPAddress(networkBytes);
                if (localNetwork.Equals(ip) || uni.Address.Equals(ip))
                {
                    return true;
                }
            }
        }

        error = $"Network/address not found on any local interface: {networkText}";
        return false;
    }
}
