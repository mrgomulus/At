namespace OBD2Suite.Models
{
    public enum ConnectionType { Serial, Bluetooth, WiFi, Simulation }
    public enum ProtocolType { Auto, ISO9141, KWP2000, CAN11bit, CAN29bit, ISO15765_11bit, ISO15765_29bit }

    /// <summary>
    /// Common baud rates used by OBD adapters.
    /// </summary>
    public static class SupportedBaudRates
    {
        /// <summary>All common OBD adapter baud rates.</summary>
        public static readonly int[] All = { 9600, 19200, 38400, 57600, 115200, 230400, 460800, 500000 };
        
        /// <summary>Default baud rate for most ELM327 adapters.</summary>
        public const int Default = 38400;
        
        /// <summary>Common rates to try during auto-detection (ordered by likelihood).</summary>
        public static readonly int[] AutoDetectSequence = { 38400, 115200, 9600, 230400, 57600, 19200 };
    }

    public class ObdConnection
    {
        public ConnectionType Type { get; set; } = ConnectionType.Serial;
        public string PortName { get; set; } = "COM3";
        public int BaudRate { get; set; } = SupportedBaudRates.Default;
        public ProtocolType Protocol { get; set; } = ProtocolType.Auto;
        public bool IsConnected { get; set; }
        public string DeviceName { get; set; } = "";
        public string ElmVersion { get; set; } = "";
        public int Timeout { get; set; } = 5000;

        // ── WiFi / Network connection ────────────────────────────────────────
        /// <summary>IP address of the WiFi OBD adapter (default ELM327 WiFi address).</summary>
        public string IpAddress { get; set; } = "192.168.0.10";

        /// <summary>TCP port for network connections (ELM327 WiFi default: 35000).</summary>
        public int NetworkPort { get; set; } = 35000;

        // ── Bluetooth connection ─────────────────────────────────────────────
        /// <summary>Bluetooth MAC address as a 12-hex-digit string (e.g. "001122334455").</summary>
        public string BluetoothAddress { get; set; } = "";
    }
}
