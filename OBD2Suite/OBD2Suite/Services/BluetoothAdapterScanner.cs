using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using OBD2Suite.Models;

namespace OBD2Suite.Services
{
    /// <summary>
    /// Discovers paired and in-range Bluetooth OBD adapters using the Windows
    /// Bluetooth API (Bthprops.cpl / irprops.cpl).
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class BluetoothAdapterScanner : IAdapterScanner
    {
        public AdapterInterface Interface => AdapterInterface.Bluetooth;

        // SPP (Serial Port Profile) service class GUID used by ELM327 adapters.
        private static readonly Guid SppServiceUuid =
            new("00001101-0000-1000-8000-00805F9B34FB");

        public async Task<IReadOnlyList<ObdAdapterInfo>> ScanAsync(
            CancellationToken cancellationToken = default)
        {
            var results = new List<ObdAdapterInfo>();
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return results;

            await Task.Run(() =>
            {
                try
                {
                    EnumerateDevices(results, remembered: true,  issueInquiry: false, cancellationToken);
                    EnumerateDevices(results, remembered: false, issueInquiry: true,  cancellationToken);

                    // Attach virtual COM port info where available
                    var comPorts = TryGetBluetoothComPorts();
                    foreach (var adapter in results)
                    {
                        if (adapter.BluetoothAddress != null &&
                            comPorts.TryGetValue(adapter.BluetoothAddress, out var comPort))
                        {
                            adapter.PortName = comPort;
                            adapter.Address  = comPort;
                        }
                    }
                }
                catch (Exception)
                {
                    // Bluetooth not available — return empty list.
                }
            }, cancellationToken);

            return results;
        }

        // ── P/Invoke helpers ────────────────────────────────────────────────

        private static void EnumerateDevices(
            List<ObdAdapterInfo> results,
            bool remembered,
            bool issueInquiry,
            CancellationToken ct)
        {
            var searchParams = new BLUETOOTH_DEVICE_SEARCH_PARAMS
            {
                dwSize                = (uint)Marshal.SizeOf<BLUETOOTH_DEVICE_SEARCH_PARAMS>(),
                fReturnAuthenticated  = true,
                fReturnRemembered     = remembered,
                fReturnUnknown        = !remembered,
                fReturnConnected      = true,
                fIssueInquiry         = issueInquiry,
                cTimeoutMultiplier    = issueInquiry ? (byte)4 : (byte)0,
                hRadio                = IntPtr.Zero
            };

            var deviceInfo = new BLUETOOTH_DEVICE_INFO
            {
                dwSize = (uint)Marshal.SizeOf<BLUETOOTH_DEVICE_INFO>()
            };

            var hFind = BluetoothFindFirstDevice(ref searchParams, ref deviceInfo);
            if (hFind == IntPtr.Zero) return;

            try
            {
                do
                {
                    ct.ThrowIfCancellationRequested();
                    var name = deviceInfo.szName.TrimEnd('\0');
                    var mac  = FormatMac(deviceInfo.Address);
                    if (!results.Any(r => r.BluetoothAddress == mac))
                    {
                        results.Add(new ObdAdapterInfo
                        {
                            Name             = string.IsNullOrWhiteSpace(name) ? $"BT OBD {mac}" : name,
                            Interface        = AdapterInterface.Bluetooth,
                            Address          = mac,
                            BluetoothAddress = mac,
                            Brand            = SerialAdapterScanner.DetectBrand(name),
                            IsAvailable      = deviceInfo.fConnected || deviceInfo.fAuthenticated,
                            Description      = $"Bluetooth OBD adapter — {mac}"
                        });
                    }
                }
                while (BluetoothFindNextDevice(hFind, ref deviceInfo));
            }
            finally
            {
                BluetoothFindDeviceClose(hFind);
            }
        }

        /// <summary>
        /// Reads the registry for Bluetooth virtual COM ports that Windows has
        /// assigned to paired RFCOMM services, mapping MAC → COM port name.
        /// </summary>
        private static Dictionary<string, string> TryGetBluetoothComPorts()
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                // Keys under HKLM\SYSTEM\CurrentControlSet\Enum\BTHENUM contain
                // BT device info; OutgoingComPort is under the device's sub-key.
                using var enumKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                    @"SYSTEM\CurrentControlSet\Enum\BTHENUM");
                if (enumKey is null) return map;

                foreach (var devClass in enumKey.GetSubKeyNames())
                {
                    using var classKey = enumKey.OpenSubKey(devClass);
                    if (classKey is null) continue;

                    foreach (var instance in classKey.GetSubKeyNames())
                    {
                        using var instKey = classKey.OpenSubKey(instance);
                        if (instKey is null) continue;

                        // Instance name format:  {service-guid}_ADDR&…
                        // Extract the BT address part after the last underscore
                        var macCandidate = ExtractMacFromBthId(instance);
                        if (macCandidate is null) continue;

                        // Look for a Device Parameters sub-key with an
                        // OutgoingComPort value.
                        using var paramKey = instKey.OpenSubKey("Device Parameters");
                        if (paramKey is null) continue;

                        var comPort = paramKey.GetValue("PortName") as string
                                   ?? paramKey.GetValue("OutgoingComPort") as string;
                        if (comPort != null)
                            map[macCandidate] = comPort;
                    }
                }
            }
            catch { /* registry read failed */ }
            return map;
        }

        private static string? ExtractMacFromBthId(string id)
        {
            // BTHENUM key names contain the address after the last '_'.
            var idx = id.LastIndexOf('_');
            if (idx < 0 || idx + 1 >= id.Length) return null;
            var raw = id[(idx + 1)..].Replace("&", "").Trim();
            // The address may be something like "AABBCCDDEEFF" or similar.
            if (raw.Length >= 12 && raw.All(s_hexChars.Contains))
                return raw[..12].ToUpperInvariant();
            return null;
        }

        private static readonly HashSet<char> s_hexChars =
            new("0123456789ABCDEFabcdef");

        private static string FormatMac(ulong addr)
        {
            var bytes = BitConverter.GetBytes(addr);
            // BT address is 6 bytes, stored little-endian in the lower 48 bits.
            return string.Concat(
                bytes[5].ToString("X2"), bytes[4].ToString("X2"),
                bytes[3].ToString("X2"), bytes[2].ToString("X2"),
                bytes[1].ToString("X2"), bytes[0].ToString("X2"));
        }

        // ── Native Bluetooth API structures ─────────────────────────────────

        [StructLayout(LayoutKind.Sequential)]
        private struct BLUETOOTH_DEVICE_SEARCH_PARAMS
        {
            public uint   dwSize;
            [MarshalAs(UnmanagedType.Bool)] public bool fReturnAuthenticated;
            [MarshalAs(UnmanagedType.Bool)] public bool fReturnRemembered;
            [MarshalAs(UnmanagedType.Bool)] public bool fReturnUnknown;
            [MarshalAs(UnmanagedType.Bool)] public bool fReturnConnected;
            [MarshalAs(UnmanagedType.Bool)] public bool fIssueInquiry;
            public byte   cTimeoutMultiplier;
            public IntPtr hRadio;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct BLUETOOTH_DEVICE_INFO
        {
            public uint  dwSize;
            public ulong Address;
            public uint  ulClassofDevice;
            [MarshalAs(UnmanagedType.Bool)] public bool fConnected;
            [MarshalAs(UnmanagedType.Bool)] public bool fRemembered;
            [MarshalAs(UnmanagedType.Bool)] public bool fAuthenticated;
            public SYSTEMTIME stLastSeen;
            public SYSTEMTIME stLastUsed;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 248)]
            public string szName;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SYSTEMTIME
        {
            public ushort wYear, wMonth, wDayOfWeek, wDay;
            public ushort wHour, wMinute, wSecond, wMilliseconds;
        }

        [DllImport("Bthprops.cpl", SetLastError = true)]
        private static extern IntPtr BluetoothFindFirstDevice(
            ref BLUETOOTH_DEVICE_SEARCH_PARAMS pbtsp,
            ref BLUETOOTH_DEVICE_INFO          pbtdi);

        [DllImport("Bthprops.cpl", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool BluetoothFindNextDevice(
            IntPtr                    hFind,
            ref BLUETOOTH_DEVICE_INFO pbtdi);

        [DllImport("Bthprops.cpl", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool BluetoothFindDeviceClose(IntPtr hFind);
    }
}
