namespace OBD2Suite.Models
{
    /// <summary>Identifies the physical interface an OBD adapter uses.</summary>
    public enum AdapterInterface { Serial, Bluetooth, Network }

    /// <summary>Well-known OBD adapter brands / chip families.</summary>
    public enum ObdAdapterBrand
    {
        Unknown, ELM327, OBDLink, Veepeak, BAFX, BlueDriver,
        iCar, Vgate, Carista, Carly, Launch, Autel
    }

    /// <summary>
    /// Represents a discovered OBD adapter regardless of its physical interface.
    /// Created by <see cref="OBD2Suite.Services.IAdapterScanner"/> implementations.
    /// </summary>
    public class ObdAdapterInfo
    {
        /// <summary>Human-readable adapter name (device name or port identifier).</summary>
        public string Name { get; set; } = "";

        /// <summary>Physical interface used to communicate with the adapter.</summary>
        public AdapterInterface Interface { get; set; }

        /// <summary>
        /// Address string: COM port for Serial, Bluetooth MAC for Bluetooth,
        /// "IP:port" for Network.
        /// </summary>
        public string Address { get; set; } = "";

        /// <summary>Identified brand / chip family (may be Unknown).</summary>
        public ObdAdapterBrand Brand { get; set; } = ObdAdapterBrand.Unknown;

        /// <summary>Whether the adapter is currently reachable.</summary>
        public bool IsAvailable { get; set; } = true;

        /// <summary>Additional human-readable description.</summary>
        public string Description { get; set; } = "";

        // ── Serial-specific ──────────────────────────────────────────────────
        /// <summary>COM port name (Serial adapters or BT virtual COM ports).</summary>
        public string? PortName { get; set; }

        /// <summary>Baud rate to use when opening the serial port.</summary>
        public int BaudRate { get; set; } = 38400;

        // ── Bluetooth-specific ───────────────────────────────────────────────
        /// <summary>Bluetooth MAC address as a 12-hex-digit string (e.g. "001122334455").</summary>
        public string? BluetoothAddress { get; set; }

        // ── Network-specific ─────────────────────────────────────────────────
        /// <summary>IPv4 or IPv6 address of the WiFi OBD adapter.</summary>
        public string? IpAddress { get; set; }

        /// <summary>TCP port the adapter listens on (default: 35000).</summary>
        public int Port { get; set; } = 35000;

        /// <summary>
        /// Creates an <see cref="ObdConnection"/> pre-filled from this adapter's properties.
        /// </summary>
        public ObdConnection ToConnection() => Interface switch
        {
            AdapterInterface.Serial => new ObdConnection
            {
                Type     = ConnectionType.Serial,
                PortName  = PortName ?? Address,
                BaudRate  = BaudRate > 0 ? BaudRate : 38400,
                DeviceName = Name
            },
            AdapterInterface.Network => new ObdConnection
            {
                Type        = ConnectionType.WiFi,
                IpAddress   = IpAddress ?? Address.Split(':')[0],
                NetworkPort = Port > 0 ? Port : 35000,
                DeviceName  = Name
            },
            AdapterInterface.Bluetooth => new ObdConnection
            {
                Type             = ConnectionType.Bluetooth,
                BluetoothAddress = BluetoothAddress ?? Address,
                PortName         = PortName,     // virtual COM port when available
                DeviceName       = Name
            },
            _ => new ObdConnection { Type = ConnectionType.Simulation }
        };
    }
}
