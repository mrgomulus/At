using System.IO.Ports;
using System.Runtime.Versioning;
using OBD2Suite.Models;

namespace OBD2Suite.Services
{
    /// <summary>
    /// Discovers OBD adapters connected via USB or physical serial port.
    /// Uses <see cref="SerialPort.GetPortNames"/> and optionally WMI device
    /// names to identify well-known OBD chip brands.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class SerialAdapterScanner : IAdapterScanner
    {
        public AdapterInterface Interface => AdapterInterface.Serial;

        // Names that typically indicate a built-in BT virtual COM port rather
        // than a real USB/serial device — filtered out so we don't duplicate BT.
        private static readonly string[] BluetoothPortHints =
            { "bluetooth", "bths", "bth" };

        public Task<IReadOnlyList<ObdAdapterInfo>> ScanAsync(
            CancellationToken cancellationToken = default)
        {
            var results = new List<ObdAdapterInfo>();

            try
            {
                // Retrieve friendly names from the registry so we can filter
                // and label each port.
                var friendlyNames = TryGetPortFriendlyNames();

                foreach (var port in SerialPort.GetPortNames().OrderBy(p => p))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    friendlyNames.TryGetValue(port, out var friendly);
                    friendly ??= port;

                    // Skip ports that look like Bluetooth virtual COM ports —
                    // those are handled by BluetoothAdapterScanner.
                    if (BluetoothPortHints.Any(h =>
                            friendly.Contains(h, StringComparison.OrdinalIgnoreCase)))
                        continue;

                    results.Add(new ObdAdapterInfo
                    {
                        Name        = $"{port} – {friendly}",
                        Interface   = AdapterInterface.Serial,
                        Address     = port,
                        PortName    = port,
                        BaudRate    = 38400,
                        Brand       = DetectBrand(friendly),
                        Description = $"USB/Serial OBD adapter on {port}",
                        IsAvailable = true
                    });
                }
            }
            catch (Exception)
            {
                // On non-Windows or access-denied, return whatever we have.
            }

            return Task.FromResult<IReadOnlyList<ObdAdapterInfo>>(results);
        }

        // ── helpers ─────────────────────────────────────────────────────────

        private static Dictionary<string, string> TryGetPortFriendlyNames()
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                    @"SYSTEM\CurrentControlSet\Enum", writable: false);
                if (key is null) return map;

                // Walk the device tree looking for COM ports
                WalkSubKeys(key, map);
            }
            catch { /* registry unavailable — fall back to plain port names */ }
            return map;
        }

        private static void WalkSubKeys(
            Microsoft.Win32.RegistryKey key,
            Dictionary<string, string> map,
            int depth = 0)
        {
            if (depth > 5) return;
            foreach (var sub in key.GetSubKeyNames())
            {
                try
                {
                    using var child = key.OpenSubKey(sub);
                    if (child is null) continue;

                    var portName = child.GetValue("PortName") as string;
                    if (portName != null && portName.StartsWith("COM", StringComparison.OrdinalIgnoreCase))
                    {
                        var friendly = child.GetValue("FriendlyName") as string
                                    ?? child.GetValue("DeviceDesc") as string
                                    ?? portName;
                        map[portName] = friendly;
                    }
                    else
                    {
                        WalkSubKeys(child, map, depth + 1);
                    }
                }
                catch { /* skip inaccessible keys */ }
            }
        }

        public static ObdAdapterBrand DetectBrand(string name)
        {
            var u = name.ToUpperInvariant();
            if (u.Contains("OBDLINK"))   return ObdAdapterBrand.OBDLink;
            if (u.Contains("VEEPEAK"))   return ObdAdapterBrand.Veepeak;
            if (u.Contains("BAFX"))      return ObdAdapterBrand.BAFX;
            if (u.Contains("BLUEDRIVER"))return ObdAdapterBrand.BlueDriver;
            if (u.Contains("ICAR"))      return ObdAdapterBrand.iCar;
            if (u.Contains("VGATE"))     return ObdAdapterBrand.Vgate;
            if (u.Contains("CARISTA"))   return ObdAdapterBrand.Carista;
            if (u.Contains("CARLY"))     return ObdAdapterBrand.Carly;
            if (u.Contains("LAUNCH"))    return ObdAdapterBrand.Launch;
            if (u.Contains("AUTEL"))     return ObdAdapterBrand.Autel;
            if (u.Contains("ELM327") || u.Contains("ELM 327")) return ObdAdapterBrand.ELM327;
            return ObdAdapterBrand.Unknown;
        }
    }
}
