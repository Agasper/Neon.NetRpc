using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Neon.Networking
{
    public class Dns
    {
        public static async Task<IPEndPoint> ResolveEndpoint(string host, int port, IPAddressSelectionRules ipAddressSelectionRules)
        {
            IPAddress ip;
            if (!IPAddress.TryParse(host, out ip))
            {
                ip = null;
                IPAddress[] addresses = await System.Net.Dns.GetHostAddressesAsync(host).ConfigureAwait(false);

                switch (ipAddressSelectionRules)
                {
                    case IPAddressSelectionRules.OnlyIpv4:
                        ip = addresses.FirstOrDefault(addr => addr.AddressFamily == AddressFamily.InterNetwork);
                        break;
                    case IPAddressSelectionRules.OnlyIpv6:
                        ip = addresses.FirstOrDefault(addr => addr.AddressFamily == AddressFamily.InterNetworkV6);
                        break;
                    case IPAddressSelectionRules.PreferIpv4:
                        ip = addresses.FirstOrDefault(addr => addr.AddressFamily == AddressFamily.InterNetwork);
                        if (ip == null)
                            ip = addresses.FirstOrDefault(addr => addr.AddressFamily == AddressFamily.InterNetworkV6);
                        break;
                    case IPAddressSelectionRules.PreferIpv6:
                        ip = addresses.FirstOrDefault(addr => addr.AddressFamily == AddressFamily.InterNetworkV6);
                        if (ip == null)
                            ip = addresses.FirstOrDefault(addr => addr.AddressFamily == AddressFamily.InterNetwork);
                        break;
                }

                if (ip == null)
                    throw new InvalidOperationException($"Couldn't resolve suitable ip address for the host {host}");
            }
            var endpoint = new IPEndPoint(ip, port);
            return endpoint;
        }
    }
}