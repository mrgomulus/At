using System.Net.NetworkInformation;
using System.Net.Sockets;
using OBD2Suite.Models;

namespace OBD2Suite.Services
{
    /// <summary>
    /// Discovers OBD adapters reachable over WiFi / Ethernet by probing
    /// well-known IP addresses and TCP ports used by ELM327 and compatible
    /// WiFi adapters (e.g. Vgate iCar, BAFX WiFi, OBDLink MX Wi-Fi).
    /// </summary>
    public class NetworkAdapterScanner : IAdapterScanner
    {
        public AdapterInterface Interface => AdapterInterface.Network;

        /// <summary>
        /// Well-known (IP, port) pairs used by ELM327-compatible WiFi adapters.
        /// </summary>
        private static readonly (string Ip, int Port, string Label)[] KnownTargets =
        {
            // ELM327 WiFi clones (most common)
            ("192.168.0.10",  35000, "ELM327 WiFi (default)"),
            ("192.168.0.10",  23,    "ELM327 WiFi (Telnet)"),
            // Vgate iCar WiFi
            ("192.168.0.10",  3000,  "Vgate iCar WiFi"),
            // BAFX Products WiFi / access-point mode
            ("192.168.1.10",  35000, "BAFX WiFi"),
            // SoftAP address used by some newer clones
            ("192.168.4.1",   35000, "ELM327 SoftAP"),
            // OBDLink MX Wi-Fi default
            ("192.168.0.1",   35000, "OBDLink MX Wi-Fi"),
            // Carlinkit / similar TCP OBD boxes
            ("192.168.10.10", 35000, "OBD TCP Adapter"),
        };

        /// <summary>Connection probe timeout in milliseconds.</summary>
        private readonly int _probeTimeoutMs;

        public NetworkAdapterScanner(int probeTimeoutMs = 1500)
        {
            _probeTimeoutMs = probeTimeoutMs;
        }

        public async Task<IReadOnlyList<ObdAdapterInfo>> ScanAsync(
            CancellationToken cancellationToken = default)
        {
            var results    = new List<ObdAdapterInfo>();
            var probeTasks = KnownTargets
                .Select(t => ProbeAsync(t.Ip, t.Port, t.Label, cancellationToken))
                .ToList();

            // Also probe devices currently in the ARP cache on the local LAN.
            var arpHosts = TryGetLocalNetworkHosts();
            foreach (var host in arpHosts)
            {
                probeTasks.Add(ProbeAsync(host, 35000, $"{host}:35000 (LAN)", cancellationToken));
                probeTasks.Add(ProbeAsync(host, 23,    $"{host}:23 (Telnet)", cancellationToken));
            }

            var found = await Task.WhenAll(probeTasks);
            results.AddRange(found.Where(r => r != null)!);

            return results;
        }

        private async Task<ObdAdapterInfo?> ProbeAsync(
            string ip, int port, string label,
            CancellationToken cancellationToken)
        {
            try
            {
                using var tcp = new TcpClient();
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(_probeTimeoutMs);

                await tcp.ConnectAsync(ip, port, cts.Token);

                return new ObdAdapterInfo
                {
                    Name        = $"{ip}:{port} – {label}",
                    Interface   = AdapterInterface.Network,
                    Address     = $"{ip}:{port}",
                    IpAddress   = ip,
                    Port        = port,
                    Brand       = DetectNetworkBrand(label),
                    IsAvailable = true,
                    Description = label
                };
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Returns distinct host IPs found in the local ARP cache that could
        /// be OBD WiFi adapters (quick, no extra network traffic).
        /// </summary>
        private static IEnumerable<string> TryGetLocalNetworkHosts()
        {
            var hosts = new HashSet<string>();
            try
            {
                // Walk all active IPv4 unicast addresses and add neighbouring
                // addresses in common small subnets (/24 within 192.168.x or 10.x).
                foreach (var iface in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (iface.OperationalStatus != OperationalStatus.Up) continue;
                    foreach (var addr in iface.GetIPProperties().UnicastAddresses)
                    {
                        if (addr.Address.AddressFamily != AddressFamily.InterNetwork) continue;
                        var parts = addr.Address.ToString().Split('.');
                        if (parts.Length != 4) continue;
                        if (!int.TryParse(parts[0], out var first)) continue;
                        if (first != 192 && first != 10) continue;

                        // Probe the first few likely OBD device addresses in the subnet.
                        foreach (var lastOctet in new[] { "1", "10", "100", "200" })
                            hosts.Add($"{parts[0]}.{parts[1]}.{parts[2]}.{lastOctet}");
                    }
                }
            }
            catch { /* network info unavailable */ }
            return hosts;
        }

        private static ObdAdapterBrand DetectNetworkBrand(string label)
        {
            var u = label.ToUpperInvariant();
            if (u.Contains("OBDLINK"))  return ObdAdapterBrand.OBDLink;
            if (u.Contains("VGATE") || u.Contains("ICAR")) return ObdAdapterBrand.Vgate;
            if (u.Contains("BAFX"))     return ObdAdapterBrand.BAFX;
            if (u.Contains("CARISTA"))  return ObdAdapterBrand.Carista;
            if (u.Contains("ELM327"))   return ObdAdapterBrand.ELM327;
            return ObdAdapterBrand.Unknown;
        }
    }
}
