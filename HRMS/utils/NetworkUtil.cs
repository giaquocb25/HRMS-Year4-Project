using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace HRMS.utils
{
    internal static class NetworkUtil
    {
        public static string GetLocalIPv4()
        {
            var candidates = NetworkInterface.GetAllNetworkInterfaces()
                .Where(adapter => adapter.OperationalStatus == OperationalStatus.Up)
                .Where(adapter => adapter.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .Where(adapter => adapter.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                .SelectMany(adapter =>
                {
                    var properties = adapter.GetIPProperties();
                    var hasGateway = properties.GatewayAddresses.Any(item =>
                        item.Address != null && item.Address.AddressFamily == AddressFamily.InterNetwork
                        && !item.Address.Equals(IPAddress.Any));
                    var isPhysicalLan = adapter.NetworkInterfaceType == NetworkInterfaceType.Ethernet
                        || adapter.NetworkInterfaceType == NetworkInterfaceType.Wireless80211
                        || adapter.NetworkInterfaceType == NetworkInterfaceType.GigabitEthernet;
                    return properties.UnicastAddresses.Select(item => new
                    {
                        item.Address,
                        HasGateway = hasGateway,
                        IsPhysicalLan = isPhysicalLan
                    });
                })
                .Where(item => item.Address.AddressFamily == AddressFamily.InterNetwork)
                .Where(item => !IPAddress.IsLoopback(item.Address) && !IsLinkLocal(item.Address))
                .OrderByDescending(item => item.IsPhysicalLan && item.HasGateway)
                .ThenByDescending(item => IsPrivate(item.Address))
                .ThenByDescending(item => item.HasGateway)
                .ToList();

            var address = candidates.Count == 0 ? null : candidates[0].Address;

            return address == null ? IPAddress.Loopback.ToString() : address.ToString();
        }

        private static bool IsLinkLocal(IPAddress address)
        {
            var bytes = address.GetAddressBytes();
            return bytes.Length == 4 && bytes[0] == 169 && bytes[1] == 254;
        }

        private static bool IsPrivate(IPAddress address)
        {
            var bytes = address.GetAddressBytes();
            return bytes.Length == 4 && (bytes[0] == 10
                || (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                || (bytes[0] == 192 && bytes[1] == 168));
        }
    }
}
