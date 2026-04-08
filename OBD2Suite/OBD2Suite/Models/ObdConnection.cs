namespace OBD2Suite.Models
{
    public enum ConnectionType { Serial, Bluetooth, WiFi, Simulation }
    public enum ProtocolType { Auto, ISO9141, KWP2000, CAN11bit, CAN29bit, ISO15765_11bit, ISO15765_29bit }

    public class ObdConnection
    {
        public ConnectionType Type { get; set; } = ConnectionType.Serial;
        public string PortName { get; set; } = "COM3";
        public int BaudRate { get; set; } = 38400;
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
