using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace BrothersBlock
{
    public static class LanRules
    {
        public const ushort Port = 7777;
        public const int PlayerLimit = 2;
        public const string Protocol = "elemental-journey-v2";

        public static bool TryAddress(string raw, out string address)
        {
            address = string.Empty;
            string candidate = (raw ?? string.Empty).Trim();
            // Require four decimal octets; IPAddress.TryParse alone also accepts abbreviated IPs.
            string[] octets = candidate.Split('.');
            if (octets.Length != 4) return false;
            foreach (string octet in octets)
            {
                byte part;
                if (octet.Length == 0 || octet.Length > 3 || !byte.TryParse(octet, out part)) return false;
                if (octet.Length > 1 && octet[0] == '0') return false;
                foreach (char character in octet) if (character < '0' || character > '9') return false;
            }
            IPAddress parsed;
            if (!IPAddress.TryParse(candidate, out parsed) || parsed.AddressFamily != AddressFamily.InterNetwork) return false;
            byte[] bytes = parsed.GetAddressBytes();
            if (bytes[0] == 0 || bytes[0] >= 224 || candidate == "255.255.255.255") return false;
            address = parsed.ToString();
            return true;
        }

        public static string Refusal(int connectedPlayers, byte[] payload)
        {
            if (payload == null || payload.Length > 64 || Encoding.UTF8.GetString(payload) != Protocol)
                return "Different game version. Install the same APK on both phones.";
            return connectedPlayers >= PlayerLimit ? "This room already has two players." : string.Empty;
        }

        public static string[] LocalAddresses()
        {
            var addresses = new List<string>();
            try
            {
                foreach (NetworkInterface adapter in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (adapter.OperationalStatus != OperationalStatus.Up || adapter.NetworkInterfaceType == NetworkInterfaceType.Loopback || adapter.NetworkInterfaceType == NetworkInterfaceType.Tunnel) continue;
                    foreach (UnicastIPAddressInformation info in adapter.GetIPProperties().UnicastAddresses)
                    {
                        if (info.Address.AddressFamily != AddressFamily.InterNetwork || IPAddress.IsLoopback(info.Address)) continue;
                        string value = info.Address.ToString();
                        if (!value.StartsWith("169.254.") && !addresses.Contains(value)) addresses.Add(value);
                    }
                }
            }
            catch (NetworkInformationException) { }
            catch (PlatformNotSupportedException) { }
            catch (SocketException) { }
            if (addresses.Count == 0)
            {
                try
                {
                    foreach (IPAddress ip in Dns.GetHostAddresses(Dns.GetHostName()))
                        if (ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip) && !addresses.Contains(ip.ToString())) addresses.Add(ip.ToString());
                }
                catch (SocketException) { }
            }
            return addresses.ToArray();
        }
    }
}
