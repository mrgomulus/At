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
    }
}
